using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

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

        private void btnStart_Click(object sender, EventArgs e)
        {
            int daysToDelete = Convert.ToInt32(cmbDeleteDays.SelectedItem);

            foreach (var folder in folders)
            {
                if (Directory.Exists(folder))
                {
                    DeleteFilesInFolder(folder, daysToDelete);
                }
            }
            // 记录文件路径到RichTextBox中
            appendRichTextBox(richTextBox1,$"任务完成！\n");

            //MessageBox.Show("任务完成！");
            if (checkBoxDaily.Checked)
            {
                ScheduleDailyTask();
            }
        }
        private void ScheduleDailyTask()
        {
            // 获取当前日期时间
            DateTime now = DateTime.Now;

            // 获取明天的日期时间
            DateTime tomorrow = now.AddDays(1).Date;

            // 计算从现在到明天零点的时间间隔
            TimeSpan timeUntilTomorrow = tomorrow - now;

            // 创建定时器并设置触发时间为明天零点
            System.Timers.Timer timer = new System.Timers.Timer(timeUntilTomorrow.TotalMilliseconds);
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
            foreach (var folder in folders)
            {
                if (Directory.Exists(folder))
                {
                    DeleteFilesInFolder(folder, daysToDelete);
                }
            }

            // 重新计算并设置下一次触发时间
            ScheduleDailyTask();
        }
        private void DeleteFilesInFolder(string folderPath, int daysToDelete)
        {
            string[] files = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories);

            foreach (var file in files)
            {
                DateTime lastWriteTime = File.GetLastWriteTime(file);
                if (DateTime.Now.Subtract(lastWriteTime).TotalDays > daysToDelete)
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
            }
        }

        private void appendRichTextBox(RichTextBox richTextBox,string text)
        {
            richTextBox.ReadOnly = false;
            richTextBox.AppendText(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " " + text);
            richTextBox.ScrollToCaret();
            richTextBox.ReadOnly = true;
        }

        private void mainform_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;  // 取消关闭操作
                this.WindowState = FormWindowState.Minimized;  // 最小化窗体
            }
        }
    }
}



