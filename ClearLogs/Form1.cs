﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ClearLogs
{
    public partial class mainform : Form
    {
        public mainform()
        {
            InitializeComponent();
            this.MinimumSize = new Size(700, 250); // 设置窗体的最小宽度和高度
        }

        private string configFilePath = "ClearConfig.ini"; // 设定档案路径
        private string[] folders; // 存储文件夹路径


        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Text += "20240130 - 创建时间 - "; // 修改为creation time
            LoadConfigFile(); // 加载设定档案
        }

        private void LoadConfigFile()
        {
            if (File.Exists(configFilePath))
            {
                folders = File.ReadAllLines(configFilePath).Distinct().ToArray();
                lstFolders.Items.Clear();
                lstFolders.Items.AddRange(folders); // 将文件夹路径添加到ListBox中
            }
            else
            {
                folders = new string[0];
                File.Create(configFilePath).Close();
            }
        }


        private void btnSetPath_Click(object sender, EventArgs e)
        {
            using (var folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    folders = new string[] { folderBrowserDialog.SelectedPath };
                    SaveConfigFile();
                }
            }
            LoadConfigFile();
        }
        private void btnClear_Click(object sender, EventArgs e)
        {
            // 清除 ini 文件中的内容
            File.WriteAllText(configFilePath, string.Empty);

            // 清空 lstFolders 中的路径
            lstFolders.Items.Clear();
        }
        private void SaveConfigFile()
        {
            File.AppendAllLines(configFilePath, folders);
            lstFolders.Items.Clear(); // 清空原有列表
            lstFolders.Items.AddRange(folders); // 将新的文件夹路径列表添加到ListBox中
        }

        private void DeleteFilesOverDays(string[] dirs,int daysToDelete)
        {
            foreach (string folder in dirs)
            {
                if (Directory.Exists(folder))
                {
                    // 处理UNC路径
                    //string path = folder.StartsWith("\\\\") ? folder.Replace("\\", "\\\\") : folder;
                    //DeleteFilesInFolder(path, daysToDelete);
                    DeleteFilesInFolder(folder, daysToDelete);
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            DisableControls();

            int daysToDelete = Convert.ToInt32(cmbDeleteDays.SelectedItem);
            #region 20240109 原代码
            //DeleteFilesOverDays(folders, daysToDelete);

            //// 记录文件路径到RichTextBox中
            //appendRichTextBox(richTextBox1,$"任务完成！\n");

            ////MessageBox.Show("任务完成！");
            //if (checkBoxDaily.Checked)
            //{
            //    ScheduleDailyTask();
            //}
            //else
            //{
            //    EnableControls();
            //}
            #endregion
            #region 20240109 异步代码
            // 创建并启动新线程
            Task.Run(() => {
                DeleteFilesOverDays(folders, daysToDelete);

                // 在主线程中更新界面
                Invoke(new Action(() =>
                {
                    // 记录文件路径到RichTextBox中
                    appendRichTextBox(richTextBox1, $"任务完成！\n");

                    if (checkBoxDaily.Checked)
                    {
                        ScheduleDailyTask();
                    }
                    else
                    {
                        EnableControls();
                    }
                }));
            });
            #endregion

        }

        #region 20240109 异步更新防止界面假死
        private void UpdateUI(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }


        #endregion

        private void ScheduleDailyTask()
        {
            // 获取当前日期时间
            DateTime now = DateTime.Now;

            // 获取明天的日期时间
            DateTime tomorrow = now.AddDays(1).Date;


            // 计算下一次运行时间
            DateTime nextRunTime = DateTime.Now.AddDays(1);
            if (nextRunTime < now)
            {
                nextRunTime.AddDays(1);
            }

            // 将下一次运行时间显示在窗体上

            #region 20240109 异步代码
            UpdateUI(() => {
                labelNextRunTime.Text = nextRunTime.ToString();
            });
            #endregion
            // 创建定时器并设置触发时间为明天
            System.Timers.Timer timer = new System.Timers.Timer(24 * 60 * 60);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // 停止定时器
            System.Timers.Timer timer = (System.Timers.Timer)sender;
            timer.Stop();

            // 执行每天一次的操作
            int daysToDelete = Convert.ToInt32(cmbDeleteDays.SelectedItem);
            DeleteFilesOverDays(folders, daysToDelete);

            // 重新计算并设置下一次触发时间
            ScheduleDailyTask();
        }
        private void DeleteFilesInFolder(string folderPath, int daysToDelete)
        {
            // 处理UNC路径
            // string path = folderPath.StartsWith("\\\\") ? folderPath.Replace("\\", "\\\\") : folderPath;
            // string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);
            

            foreach (var file in files)
            {
                DateTime creatTime = File.GetCreationTime(file);
                if (DateTime.Now.Subtract(creatTime).TotalDays > daysToDelete)
                {
                    try
                    {
                        // 删除文件
                        File.Delete(file);

                        // 记录文件路径到RichTextBox中
                        appendRichTextBox(richTextBox1, $"删除文件[{file}]成功！\n");
                    }
                    catch (Exception ex) 
                    {
                        // 记录文件路径到RichTextBox中
                        appendRichTextBox(richTextBox1, $"删除文件[{file}]失败！{ex}\n");
                    }
                }
                else if (DateTime.Now.Subtract(creatTime).TotalDays < daysToDelete && creatTime.Date != DateTime.Now.Date)
                {
                    if (!checkBoxMove.Checked)                    
                        continue;                        
                    
                    // 小于 daysToDelete 天的且不是当天创建的文件按日期分类
                    string subfolderPath = Path.Combine(folderPath, creatTime.ToString("yyyyMM"), creatTime.ToString("yyyyMMdd"));

                    if (!Directory.Exists(subfolderPath))
                    {
                        // 创建日期文件夹
                        Directory.CreateDirectory(subfolderPath);
                    }

                    // 移动文件到日期文件夹
                    string newFilePath = Path.Combine(subfolderPath, Path.GetFileName(file));

                    try
                    {
                        File.Move(file, newFilePath);
                        appendRichTextBox(richTextBox1, $"移动文件[{file}]到[{subfolderPath}]成功！\n");
                    }
                    catch (Exception ex)
                    {
                        appendRichTextBox(richTextBox1, $"移动文件[{file}]到[{subfolderPath}]失败！{ex}\n");
                    }
                }
            }

            // 删除空文件夹
            DeleteEmptyFolders(folderPath);
        }
        private void DeleteEmptyFolders(string rootFolder)
        {
            // 获取所有空文件夹
            string[] emptyFolders = Directory.GetDirectories(rootFolder, "*", SearchOption.AllDirectories)
                                              .Where(dir => Directory.GetFiles(dir).Length == 0 &&
                                                            Directory.GetDirectories(dir).Length == 0)
                                              .ToArray();

            // 逐个删除空文件夹
            foreach (var emptyFolder in emptyFolders)
            {
                try
                {
                    Directory.Delete(emptyFolder);
                    appendRichTextBox(richTextBox1, $"删除空文件夹[{emptyFolder}]成功！\n");
                }
                catch (Exception ex)
                {
                    appendRichTextBox(richTextBox1, $"删除空文件夹[{emptyFolder}]失败！{ex}\n");
                }
            }
        }
        #region 20240109 appendRichTextBox原代码
        //private void appendRichTextBox(RichTextBox richTextBox,string text)
        //{
        //    richTextBox.ReadOnly = false;
        //    richTextBox.AppendText(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + text);
        //    richTextBox.ScrollToCaret();
        //    richTextBox.ReadOnly = true;
        //}
        #endregion
       
        #region 20240109 appendRichTextBox异步代码
        private void appendRichTextBox(RichTextBox richTextBox, string text)
        {
            UpdateUI(() => {
                richTextBox.ReadOnly = false;
                richTextBox.AppendText(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + text);
                richTextBox.ScrollToCaret();
                richTextBox.ReadOnly = true;
            });

        }
        #endregion


        private void mainform_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;  // 取消关闭操作
                this.WindowState = FormWindowState.Minimized;  // 最小化窗体
            }
        }
        private void DisableControls()
        {
            btnStart.Enabled = false;
            btnSetPath.Enabled = false;
            btnClear.Enabled = false;
            //lstFolders.Enabled = false;
            
            cmbDeleteDays.Enabled = false;
            checkBoxDaily.Enabled = false;
            labelNextRunTime.Enabled = false;
            checkBoxMove.Enabled = false;
        }

        private void EnableControls()
        {
            btnStart.Enabled = true;
            btnSetPath.Enabled = true;
            btnClear.Enabled = true;
            //lstFolders.Enabled = true;
            cmbDeleteDays.Enabled = true;
            checkBoxDaily.Enabled = true;
            labelNextRunTime.Enabled = true;
            checkBoxMove.Enabled = true;
        }

        private void lstFolders_DoubleClick(object sender, EventArgs e)
        {
            
            if (lstFolders.SelectedIndex != -1)
            {
                string folderPath = lstFolders.SelectedItem.ToString();
                OpenFolder(folderPath);
            }
            
        }
        private void OpenFolder(string folderPath)
        {
            try
            {
                // 使用 Process.Start 方法打开选定的文件夹
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开文件夹失败！{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }


}



