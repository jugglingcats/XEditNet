<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

  <xsl:import href="file:///c:/java/docbook-xsl-1.70.0/htmlhelp/htmlhelp.xsl"/>

<!--
  <xsl:param name="img.src.path">..\..\..\..\XEditNetCtrl\</xsl:param>
-->
  <xsl:param name="admon.style"/>
  <xsl:param name="chapter.autolabel" select="0"/>
  <xsl:param name="part.autolabel" select="0"/>
  <xsl:param name="html.stylesheet" select="'htmlhelp.css'"/>
  <xsl:param name="chunk.section.depth" select="4"/>
  <xsl:param name="chunk.first.sections" select="1"/>
  <xsl:param name="generate.section.toc.level" select="0"/>
  <xsl:param name="toc.section.depth" select="1"/>
  <xsl:param name="header.rule" select="0"/>
  <xsl:param name="generate.toc">
	book toc
	reference toc
	part toc
	section toc
  </xsl:param>
<!--
  <xsl:param name="suppress.navigation" select="0"/>
-->

</xsl:stylesheet>

