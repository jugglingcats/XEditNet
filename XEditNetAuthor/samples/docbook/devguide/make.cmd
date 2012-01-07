set SAXON=java -jar c:/java/saxon/saxon.jar

%SAXON% ..\devguide.xml driver.xsl "htmlhelp.chm=devguide.chm"
"c:\Program Files\HTML Help Workshop\hhc.exe" htmlhelp.hhp
