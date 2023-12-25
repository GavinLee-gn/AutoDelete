@echo off
chcp 65001

cd "%cd%"
echo 此文件夹("%cd%")所有文件均会被上传，请确认后再点击

pause

cls
echo 输入commit
set /p commit=
:loop


cls

git add *
pause
git status
pause
cls

git commit -m "%commit%"

cls
rem 免输密码：只需运行一次
rem https://www.cnblogs.com/1050619969kong/p/15103450.html

rem echo 输入仓库地址(格式类似https://gitee.com/xxxxx/xxxx.git)
set remotePath=https://gitee.com/Gavin_bx/auto-delete.git

git push %remotePath% master

pause


::goto loop
