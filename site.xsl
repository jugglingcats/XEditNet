<xsl:stylesheet version="1.0"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns="http://www.w3.org/1999/xhtml"
	xmlns:exsl="http://exslt.org/common" exclude-result-prefixes="exsl"
>
<xsl:variable name="FONT_DEFAULT">Verdana, Helvetica, Sans Serif</xsl:variable>
<xsl:variable name="WIDTH_MIDCONTAINER">496</xsl:variable>

<xsl:output
   method="html"
   indent="yes"
   encoding="iso-8859-1"
/>

<xsl:template match="website">
	<xsl:apply-templates/>
</xsl:template>

<xsl:template match="page">
	<exsl:document method="html" href="{@id}.html">
		<xsl:choose>
		<xsl:when test="*//section">
			<xsl:call-template name="ChapterContainerPage"/>
		</xsl:when>
		<xsl:otherwise>
			<xsl:call-template name="BodyContainerPage"/>
		</xsl:otherwise>
		</xsl:choose>
	</exsl:document>
</xsl:template>

<xsl:template match="section">
	<exsl:document method="html" href="{@id}.html">
		<xsl:call-template name="SectionContainerPage"/>
	</exsl:document>
</xsl:template>

<xsl:template match="contact | secttitle">
</xsl:template>

<xsl:template match="h1">
	<p>
		<font size="3"><b><xsl:apply-templates/></b></font>
	</p>
</xsl:template>

<xsl:template match="page[@id='people']">
	<exsl:document method="html" href="{@id}.html">
		<xsl:call-template name="PeopleContainerPage"/>
	</exsl:document>
</xsl:template>

<xsl:template match="photo">
	<img>
	    <xsl:apply-templates select="@*"/>
		<xsl:apply-templates/>
	</img>
</xsl:template>

<xsl:template match="news">
	<p>
		<a name="{@id}"/>
		<b><xsl:value-of select="newstitle"/></b>
		<img src="images/spacer.gif" width="5" height="1"/>
		<nobr><xsl:value-of select="newsdate"/></nobr><br
		/><xsl:apply-templates select="newsbody"/>
	</p>
	<p></p>
</xsl:template>

<xsl:template match="td | th">
	<xsl:copy>
		<xsl:apply-templates select="@*"/>
		<xsl:if test="not(@valign)">
			<xsl:attribute name="valign">top</xsl:attribute>
		</xsl:if>
		<xsl:if test="not(@align)">
			<xsl:attribute name="align">left</xsl:attribute>
		</xsl:if>
		<font face="{$FONT_DEFAULT}" size="2">
			<xsl:apply-templates select="node()"/>
		</font>
	</xsl:copy>
</xsl:template>

<xsl:template match="img">
	<xsl:copy>
		<xsl:apply-templates select="@*"/>
		<xsl:attribute name="border">0</xsl:attribute>
	</xsl:copy>
</xsl:template>

<xsl:template match="dl">
	<table width="100%" border="0">
	<xsl:apply-templates/>
	</table>
</xsl:template>

<xsl:template match="dt">
	<tr><td colspan="2">
	<font size="2"><b><xsl:apply-templates/></b></font>
	</td></tr>
</xsl:template>

<xsl:template match="dd">
	<tr>
		<td width="20"><img src="images/spacer.gif" width="20" height="1"/></td>
		<td width="100%"><font size="2"><xsl:apply-templates/></font></td>
	</tr>
	<tr>
		<td colspan="2"><img src="images/spacer.gif" width="1" height="8"/></td>
	</tr>
</xsl:template>

<xsl:template name="RecentNews">
	<table border="0" cellpadding="0" cellspacing="0" width="150">
		<xsl:apply-templates mode="newstoc" select="//news"/>
		<tr>
			<xsl:call-template name="SpacerTD"/>
			<td><img src="images/spacer.gif" height="5" width="1"/><br/>
				<font face="{$FONT_DEFAULT}" size="2">
					<a href="news.html"><img border="0" src="images/details.gif"/>
					<b>All News</b></a>
				</font>
				<br/>
				<br/>
			</td>
			<xsl:call-template name="SpacerTD"/>
		</tr>		
	</table>
</xsl:template>

<xsl:template mode="newstoc" match="news">
	<xsl:if test="not(position()>3)">
		<tr>
			<xsl:call-template name="SpacerTD"/>
			<td>
				<img src="images/spacer.gif" height="5" width="1"/><br/>
				<font face="{$FONT_DEFAULT}" size="2">
					<xsl:apply-templates mode="newstoc"/>
					<a href="news.html#{@id}">
					<img border="0" src="images/details.gif"/>details
					</a>
				</font>
			</td>
			<xsl:call-template name="SpacerTD"/>
		</tr>
	</xsl:if>
</xsl:template>

<xsl:template mode="newstoc" match="newstitle">
	<font size="2"><b><xsl:apply-templates/></b></font><br/>
</xsl:template>

<xsl:template mode="newstoc" match="newsdate">
	<font color="#333333" size="2">
	<xsl:apply-templates/>
	</font><br/>
</xsl:template>

<xsl:template mode="newstoc" match="newssummary">
	<font size="2"><xsl:apply-templates/></font>
</xsl:template>

<xsl:template mode="newstoc" match="newsbody">
</xsl:template>

<xsl:template mode="topnav" match="page">
	<xsl:param name="selected"/>
	<xsl:if test="@id != 'news'">
		<img src="images/spacer.gif" width="5" height="1"/>
		<xsl:if test="preceding-sibling::page">
			|<img src="images/spacer.gif" width="5" height="1"/>
		</xsl:if>
		<xsl:choose>
			<xsl:when test="@id=$selected">
				<font color="gray" face="{$FONT_DEFAULT}" size="2"><b>
				<xsl:value-of select="navtitle"/>
				</b></font>
			</xsl:when>
			<xsl:otherwise>
				<a class="topnav" href="{@id}.html">
				<font face="{$FONT_DEFAULT}" size="2"><b>
				<xsl:value-of select="navtitle"/>
				</b></font></a>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:if>
</xsl:template>

<xsl:template name="BodyContainerPage">
<html>
	<xsl:call-template name="HeadForPage"/>

	<body alink="#3333ff" vlink="#000099" link="#000099">

	<table height="100%" align="center" border="0" width="806" cellpadding="0" cellspacing="0">
		<xsl:call-template name="BannerTopStandard"/>

		<xsl:call-template name="TitleTRs">
			<xsl:with-param name="leftTitle">Screenshots</xsl:with-param>
			<xsl:with-param name="centreTitle"><xsl:value-of select="pagetitle"/></xsl:with-param>
			<xsl:with-param name="rightTitle">What's New</xsl:with-param>
		</xsl:call-template>
		
		<tr>
			<td class="leftCol" valign="top" width="160">
				<xsl:call-template name="LeftContainerContact"/>
			</td>
			<td class="centreCol" valign="top" width="486">
				<table border="0" width="100%" cellpadding="0" cellspacing="0">
					<tr>
						<xsl:call-template name="SpacerTD"/>
						<td width="{$WIDTH_MIDCONTAINER}">
							<font face="{$FONT_DEFAULT}" size="2">
								<xsl:apply-templates select="pagebody/*"/>
							</font>
						</td>
						<xsl:call-template name="SpacerTD"/>
					</tr>
				</table>
			</td>
			<td class="rightCol" valign="top">
				<xsl:call-template name="RecentNews"/>
			</td>
		</tr>
		<xsl:call-template name="PageEndStandard"/>
	</table>

	</body>
</html>
</xsl:template>


<xsl:template name="ChapterContainerPage">
<html>
	<xsl:call-template name="HeadForPage"/>

	<body topmargin="4" leftmargin="4" alink="#3333ff" vlink="#000099" link="#000099">

	<table height="100%" align="center" border="0" width="806" cellpadding="0" cellspacing="0">
		<xsl:call-template name="BannerTopStandard"/>

		<xsl:call-template name="TitleTRs">
			<xsl:with-param name="leftTitle">Contents</xsl:with-param>
			<xsl:with-param name="centreTitle"><xsl:value-of select="pagetitle"/></xsl:with-param>
			<xsl:with-param name="rightTitle">What's New</xsl:with-param>
		</xsl:call-template>
		
		<tr>
			<td class="leftCol" valign="top" width="160">
				<xsl:call-template name="TocTABLE">
					<xsl:with-param name="parentid"><xsl:value-of select="@id"/></xsl:with-param>
				</xsl:call-template>
			</td>
			<td class="centreCol" valign="top" width="486">
				<table border="0" width="100%" cellpadding="0" cellspacing="0">
					<tr>
						<xsl:call-template name="SpacerTD"/>
						<td width="{$WIDTH_MIDCONTAINER}">
							<font face="{$FONT_DEFAULT}" size="2">
								<xsl:apply-templates select="pagebody/*"/>
							</font>
						</td>
						<xsl:call-template name="SpacerTD"/>
					</tr>
				</table>
			</td>
			<td class="rightCol" valign="top">
				<xsl:call-template name="RecentNews"/>
			</td>
		</tr>
		<xsl:call-template name="PageEndStandard"/>
	</table>

	</body>
</html>
</xsl:template>

<xsl:template name="SectionContainerPage">
<html>
	<xsl:call-template name="HeadForPage"/>

	<body topmargin="4" leftmargin="4" alink="#3333ff" vlink="#000099" link="#000099">

	<table height="100%" align="center" border="0" width="806" cellpadding="0" cellspacing="0">
		<xsl:call-template name="BannerTopStandard"/>

		<xsl:call-template name="TitleTRs">
			<xsl:with-param name="leftTitle">Contents</xsl:with-param>
			<xsl:with-param name="centreTitle"><xsl:value-of select="secttitle"/></xsl:with-param>
			<xsl:with-param name="rightTitle">What's New</xsl:with-param>
		</xsl:call-template>
		
		<tr>
			<td class="leftCol" valign="top" width="160">
				<xsl:call-template name="TocTABLE">
					<xsl:with-param name="parentid">
						<xsl:value-of select="../../@id"/>
					</xsl:with-param>
					<xsl:with-param name="currentid">
						<xsl:value-of select="@id"/>
					</xsl:with-param>
				</xsl:call-template>
			</td>
			<td class="centreCol" valign="top" width="486">
				<table border="0" width="100%" cellpadding="0" cellspacing="0">
					<tr>
						<xsl:call-template name="SpacerTD"/>
						<td width="{$WIDTH_MIDCONTAINER}">
							<font face="{$FONT_DEFAULT}" size="2">
								<xsl:apply-templates/>
							</font>
						</td>
						<xsl:call-template name="SpacerTD"/>
					</tr>
				</table>
			</td>
			<td class="rightCol" valign="top">
				<xsl:call-template name="RecentNews"/>
			</td>
		</tr>
		<xsl:call-template name="PageEndStandard"/>
	</table>

	</body>
</html>
</xsl:template>

<xsl:template name="HeadForPage">
	<xsl:comment>

	** Generated from XML using XSLT
	** Document and stylesheet source available in this directory:
	**   site.xml
	**   site.xsl
	**
	** Alfie Kirkpatrick
	** 18 October, 2004

	</xsl:comment>
	<head>
		<title>
			<xsl:value-of select="pagetitle"/>
		</title>
		<link href="css/generic.css" rel="stylesheet" type="text/css"/>
	</head>
</xsl:template>

<xsl:template name="BannerTopStandard">
		<tr height="80">
			<td colspan="5" valign="top">
				<table cellpadding="0" cellspacing="0" border="0" width="100%">
				<tr height="1">
					<td width="85">
						<img src="images/icon_truecol.png"/>
					</td>
					<td>
						<img src="images/logo_truecol.png"/>
					</td>
					<td align="right" valign="bottom">
						<nobr>
						<xsl:apply-templates mode="topnav" select="//page">
							<xsl:with-param name="selected">
								<xsl:value-of select="@id"/>
							</xsl:with-param>
						</xsl:apply-templates>
						</nobr>
					</td>
				</tr>
				</table>
			</td>
		</tr>
<!--
		<tr height="10">
			<td class="topBar" colspan="5">
				<nobr>
					<xsl:apply-templates mode="topnav" select="//page">
						<xsl:with-param name="selected">
							<xsl:value-of select="@id"/>
						</xsl:with-param>
					</xsl:apply-templates>
				</nobr>
			</td>
		</tr>
-->
		<tr height="1">
			<td colspan="5"><img src="images/spacer.gif" width="806" height="10"/></td>
		</tr>
</xsl:template>

<xsl:template name="LeftContainerContact">
	<table border="0" width="158" align="center" cellpadding="0" cellspacing="0">
		<tr>
			<td colspan="3" valign="top" align="center" bgcolor="#D2DEE4">
			<font face="{$FONT_DEFAULT}" size="2">
				<xsl:apply-templates select="//div[@class='contact']"/>
			</font>
			</td>
		</tr>
	</table>
</xsl:template>

<xsl:template name="TocTABLE">
	<xsl:param name="parentid"/>
	<xsl:param name="currentid"/>

	<table align="right" border="0" width="159" cellpadding="0" cellspacing="0">
		<xsl:for-each select="//page[@id=$parentid]//section">

			<xsl:choose>
			<xsl:when test="@id=$currentid">
			<tr bgcolor="#6699ff">
				<td colspan="4"><img src="images/spacer.gif" width="1" height="3"/></td>
				<td bgcolor="#6699ff" width="5"
					><img src="images/spacer.gif" width="5" height="1"/></td>
			</tr>
			<tr bgcolor="#6699ff">
				<xsl:call-template name="SpacerTD"/>
				<td></td>
				<td>
					<a href="{@id}.html"><font face="{$FONT_DEFAULT}" size="2" color="white">
						<xsl:value-of select="secttitle"/>
					</font></a>
				</td>
				<xsl:call-template name="SpacerTD"/>
				<td bgcolor="#6699ff" width="5"
					><img src="images/spacer.gif" width="5" height="1"/></td>
			</tr>
			<tr bgcolor="#6699ff">
				<td colspan="4"><img src="images/spacer.gif" width="1" height="3"/></td>
				<td bgcolor="#6699ff" width="5"
					><img src="images/spacer.gif" width="5" height="1"/></td>
			</tr>
			</xsl:when>

			<xsl:otherwise>
			<tr bgcolor="#f0f0f0">
				<td colspan="4"><img src="images/spacer.gif" width="1" height="3"/></td>
				<td bgcolor="#6699ff" width="5"
					><img src="images/spacer.gif" width="5" height="1"/></td>
			</tr>
			<tr bgcolor="#f0f0f0">
				<xsl:call-template name="SpacerTD"/>
				<td width="6" valign="top"><font face="{$FONT_DEFAULT}" size="2"
					><nobr>&#160;<a href="{@id}.html"
					><img src="images/details.gif" border="0" width="12" height="8"/></a></nobr
					></font></td>
				<td valign="top">
					<a href="{@id}.html"><font face="{$FONT_DEFAULT}" size="2">
						<xsl:value-of select="secttitle"/>
					</font></a>
				</td>
				<xsl:call-template name="SpacerTD"/>
				<td bgcolor="#6699ff" width="5"
					><img src="images/spacer.gif" width="5" height="1"/></td>
			</tr>
			<tr bgcolor="#f0f0f0">
				<td colspan="4"><img src="images/spacer.gif" width="1" height="3"/></td>
				<td bgcolor="#6699ff" width="5"
					><img src="images/spacer.gif" width="5" height="1"/></td>
			</tr>
			</xsl:otherwise>
			</xsl:choose>

		</xsl:for-each>
		<tr>
			<td colspan="5"><img src="images/spacer.gif" width="1" height="100"/></td>
		</tr>
	</table>
</xsl:template>

<xsl:template name="SpacersTR">
	<tr>
		<td width="1">
			<img src="images/spacer.gif" width="1" height="1"/>
		</td>
		<td width="150">
			<img src="images/spacer.gif" width="150" height="4"/>
		</td>
		<td width="1">
			<img src="images/spacer.gif" width="1" height="1"/>
		</td>
		<td width="{$WIDTH_MIDCONTAINER}">
			<img src="images/spacer.gif" width="{$WIDTH_MIDCONTAINER}" height="1"/>
		</td>
		<td width="1">
			<img src="images/spacer.gif" width="1" height="1"/>
		</td>
		<td width="150">
			<img src="images/spacer.gif" width="150" height="1"/>
		</td>
		<td width="1">
			<img src="images/spacer.gif" width="1" height="1"/>
		</td>
	</tr>
</xsl:template>

<xsl:template name="PageEndStandard">
	<tr height="1">
		<td class="baseLeft" align="center" height="7"><img src="images/spacer.gif" height="1" width="1" /></td>
		<td class="baseCenter" height="7"><img src="images/spacer.gif" height="1" width="1" /></td>
		<td class="baseRight" height="7"><img src="images/spacer.gif" height="1" width="1" /></td>
	</tr>
	<tr height="1">
		<td colspan="5">
			<img src="images/spacer.gif" width="1" height="1"/>
		</td>
	</tr>
	<tr height="1">
		<td colspan="5">
			<table width="100%" border="0" cellpadding="5" cellspacing="0">
				<tr>
					<td>
						<font face="{$FONT_DEFAULT}" size="2">
						&#169; Copyright 2004, XEditNet Ltd
						</font>
					</td>
				</tr>
			</table>
		</td>
	</tr>
</xsl:template>

<xsl:template name="TitleTRs">
	<xsl:param name="leftTitle">NO TITLE</xsl:param>
	<xsl:param name="centreTitle">NO TITLE</xsl:param>
	<xsl:param name="rightTitle">NO TITLE</xsl:param>

	<tr height="1">
		<td class="topLeft" height="20" width="160">
			<div class="pad5"><span class="headline1"><xsl:value-of select="$leftTitle"/></span></div>
		</td>
		<td width="5" rowspan="3"><img src="images/spacer.gif" width="5" height="1" /></td>
		<td class="topCenter" height="20" width="486">
			<div class="pad5"><span class="headline1"><xsl:value-of select="$centreTitle"/></span></div>
		</td>
		<td width="5" rowspan="3"><img src="images/spacer.gif" width="5" height="1" /></td>
		<td class="topRight" height="20" width="150">
			<div class="pad5"><span class="headline1"><xsl:value-of select="$rightTitle"/></span></div>
		</td>
	</tr>
</xsl:template>

<xsl:template name="SpacerTD">
	<td width="5"><img src="images/spacer.gif" width="5" height="1"/></td>
</xsl:template>

<xsl:template name="VerticalDashTD">
	<td background="images/verdash.gif" width="1">
		<img src="images/spacer.gif" width="1" height="1"/>
	</td>
</xsl:template>

<xsl:template match="*|@*">
  <xsl:copy>
    <xsl:apply-templates select="@*"/>
    <xsl:apply-templates select="node()"/>
  </xsl:copy>
</xsl:template>

</xsl:stylesheet>
