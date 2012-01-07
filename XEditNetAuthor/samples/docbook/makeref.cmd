set SAXON=java -jar c:/java/saxon/saxon.jar

D:\alfie\vsproj\AssemblyDoc\bin\release\AssemblyDoc.exe ..\..\..\XEditNetCtrl\bin\Debug\XEditNetCtrl.dll ..\..\..\XEditNetCtrl\codedoc.xml > codedoc_merged.xml
%SAXON% codedoc_merged.xml cd2doc.xsl > codedoc.xmlfrag