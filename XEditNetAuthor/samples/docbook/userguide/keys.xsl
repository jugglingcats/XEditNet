<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

	<xsl:output method="xml" omit-xml-declaration="yes"/>
	
	<xsl:template match="/">
		<informaltable>
			<xsl:apply-templates select="//KeyMapping">
				<xsl:sort select="@Method"/>	
			</xsl:apply-templates>
		</informaltable>
	</xsl:template>
	
	<xsl:template match="KeyMapping">
		<tr>
			<td><xsl:value-of select="@Method"/></td>
			<td><xsl:value-of select="@Sequence"/></td>
		</tr>
	</xsl:template>
	
</xsl:stylesheet>

