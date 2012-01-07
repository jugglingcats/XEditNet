rem set SAXON=java -cp c:/java/saxon/saxon.jar com.icl.saxon.StyleSheet
set SAXON=java -jar c:/java/saxon/saxon.jar

copy /y ..\..\..\..\XEditNetCtrl\widgets\*.bmp widgets
%SAXON% ..\..\..\..\XEditNetCtrl\keys.xml keys.xsl > ..\key_info.xmlfrag
%SAXON% ..\userguide.xml driver.xsl "htmlhelp.chm=userguide.chm" use.extensions=1
"c:\Program Files\HTML Help Workshop\hhc.exe" htmlhelp.hhp
