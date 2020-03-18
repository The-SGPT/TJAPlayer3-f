@echo off
for /l %%i in (0,1,7) do (
set /a jjj=0
set /a jjj=%%i+1
echo %%i
echo %jjj%
copy "bar_center_genre_%%i.png" "Bar_Center_Genre_%jjj%.png" 
)