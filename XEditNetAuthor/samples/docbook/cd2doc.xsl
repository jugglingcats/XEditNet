<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.1">

	<xsl:template match="assembly">
		<chapter>
			<title><xsl:value-of select="@name"/> Reference</title>
			<xsl:apply-templates>
				<xsl:sort select="@name"/>
			</xsl:apply-templates>
		</chapter>
	</xsl:template>

	<xsl:template match="namespace[class|enum|interface]">
		<sect1>
			<title>Namespace <xsl:value-of select="@name"/></title>
			
			<xsl:if test="class|struct|enum|interface">
				<informaltable>
					<xsl:for-each select="class|struct|enum|interface">
						<xsl:sort select="@name"/>
						<tr>
							<td>
								<link>
									<xsl:attribute name="linkend"><xsl:value-of select="@fullname"/></xsl:attribute>
									<xsl:value-of select="@name"/>
									<xsl:call-template name="TypeSwitch"/>
								</link>
							</td>
							<td>
								<xsl:apply-templates select="summary/para[1]/text()"/>
							</td>
						</tr>
					</xsl:for-each>
				</informaltable>
			</xsl:if>

			<xsl:apply-templates>
				<xsl:sort select="@name"/>
			</xsl:apply-templates>
		</sect1>
	</xsl:template>

	<xsl:template match="namespace"/>
		
	<xsl:template match="hierarchy">
		<xsl:apply-templates/>
	</xsl:template>

	<xsl:template match="super">
		<blockquote role="super">
			<xsl:apply-templates/>
			<xsl:if test="not(super)">
				<xsl:variable name="class"><xsl:value-of select="ancestor::class[1]/@fullname"/></xsl:variable>
				<blockquote role="super">
					<para>
						<emphasis role="bold">
							<xsl:value-of select="$class"/>
						</emphasis>
					</para>
					<xsl:variable name="derived" select="//class[hierarchy/descendant::*[last()][@fullname=$class]]"/>
					<xsl:if test="$derived">
						<blockquote>
							<para>
								<link linkend="{$derived/@fullname}">
									<xsl:value-of select="$derived/@fullname"/>
								</link>
							</para>
						</blockquote>
					</xsl:if>
				</blockquote>
			</xsl:if>
		</blockquote>
	</xsl:template>
	
	<xsl:template match="type">
		<para><xsl:value-of select="@fullname"/></para>
	</xsl:template>

	<xsl:template match="ref">
		<para><link linkend="{@fullname}"><xsl:value-of select="@fullname"/></link></para>
	</xsl:template>

	<xsl:template match="class|struct|interface">
		<sect2 id="{@fullname}">
			<title>
				<xsl:value-of select="@name"/>
				<xsl:call-template name="TypeSwitch"/>
			</title>
			<xsl:apply-templates select="summary/*"/>
			<xsl:if test="constructorInfo|methodInfo|propertyInfo|fieldInfo|eventInfo">
				<para>For a list of all members of this type, see <link linkend="{@fullname}-members">
					<xsl:value-of select="@name"/> Members</link>.</para>
			</xsl:if>
			<xsl:apply-templates select="hierarchy"/>
			<classsynopsis>
				<ooclass>
					<modifier><xsl:value-of select="@modifiers"/></modifier>
					<classname><xsl:value-of select="@name"/></classname>
				</ooclass>
			</classsynopsis>
			<xsl:if test="remarks">
				<bridgehead>Remarks</bridgehead>
				<xsl:apply-templates select="remarks/*"/>
			</xsl:if>
			<xsl:if test="constructorInfo|methodInfo|propertyInfo|fieldInfo|eventInfo">
				<sect3 id="{@fullname}-members">
					<title><xsl:value-of select="@name"/> Members</title>
					<xsl:apply-templates mode="member-toc" select="constructorInfo|methodInfo|propertyInfo|fieldInfo|eventInfo"/>
					<xsl:apply-templates select="(constructorInfo|methodInfo|propertyInfo|fieldInfo|eventInfo)/*"/>
				</sect3>
			</xsl:if>
		</sect2>
	</xsl:template>

	<xsl:template match="enum">
		<sect2 id="{@fullname}">
			<title>
				<xsl:value-of select="@name"/>
				<xsl:call-template name="TypeSwitch"/>
			</title>
			<xsl:apply-templates select="summary/*"/>
			<classsynopsis>
				<ooclass>
					<modifier><xsl:value-of select="@modifiers"/></modifier>
					<classname><xsl:value-of select="@name"/></classname>
				</ooclass>
			</classsynopsis>
			<bridgehead>Members</bridgehead>
			<informaltable>
				<xsl:for-each select="fieldInfo/field">
					<xsl:sort select="@name"/>
					<tr>
						<td><xsl:value-of select="@name"/></td>
						<td><xsl:value-of select="summary/*"/></td>
					</tr>
				</xsl:for-each>
			</informaltable>
			<xsl:if test="remarks">
				<bridgehead>Remarks</bridgehead>
				<xsl:apply-templates select="remarks/*"/>
			</xsl:if>
		</sect2>
	</xsl:template>

	<xsl:template name="TypeSwitch">
		<xsl:text> </xsl:text>
		<xsl:if test="name()='class'">Class</xsl:if>
		<xsl:if test="name()='struct'">Structure</xsl:if>
		<xsl:if test="name()='enum'">Enumeration</xsl:if>
		<xsl:if test="name()='interface'">Interface</xsl:if>
	</xsl:template>
	
	<xsl:template name="MemberSwitch">
		<xsl:text> </xsl:text>
		<xsl:if test="name()='method'">Method</xsl:if>
		<xsl:if test="name()='property'">Property</xsl:if>
		<xsl:if test="name()='field'">Field</xsl:if>
		<xsl:if test="name()='event'">Event</xsl:if>
	</xsl:template>
	
	<xsl:template mode="member-toc" match="constructorInfo|methodInfo|propertyInfo|fieldInfo|eventInfo">
		<xsl:choose>
			<xsl:when test="name()='constructorInfo'">
				<bridgehead>Constructors</bridgehead>
			</xsl:when>
			<xsl:when test="name()='methodInfo'">
				<bridgehead>Methods</bridgehead>
			</xsl:when>
			<xsl:when test="name()='propertyInfo'">
				<bridgehead>Properties</bridgehead>
			</xsl:when>
			<xsl:when test="name()='fieldInfo'">
				<bridgehead>Fields</bridgehead>
			</xsl:when>
			<xsl:when test="name()='eventInfo'">
				<bridgehead>Events</bridgehead>
			</xsl:when>
		</xsl:choose>
		<variablelist>
			<xsl:for-each select="method|property|field|event|baseRef">
				<xsl:sort select="@name"/>
				<varlistentry>
					<xsl:choose>
						<xsl:when test="name()='method' or name()='property' or name()='field' or name()='event'">
							<term>
								<link linkend="{@id}">
									<xsl:value-of select="@sig"/>
								</link>
								<xsl:if test="@base">
									(inherited from <emphasis role="bold"><xsl:value-of select="@base"/></emphasis>)
								</xsl:if>
							</term>
							<listitem><para>
								<xsl:if test="@overridden = 'true'">
								Overridden.
								</xsl:if>
								<xsl:apply-templates select="summary/para[1]/text()"/>
							</para></listitem>
						</xsl:when>
						<xsl:when test="@idref">
							<term>
								<link linkend="{@idref}">
									<xsl:value-of select="@sig"/>
								</link>
								<xsl:if test="@base">
									(inherited from <emphasis role="bold"><xsl:value-of select="@base"/></emphasis>)
								</xsl:if>
							</term>
							<listitem><para>
								<xsl:if test="@overridden = 'true'">
								Overridden.
								</xsl:if>
								<xsl:variable name="idref" select="@idref"/>
								<xsl:apply-templates select="//method[@id=$idref]/summary/para[1]/text()"/>
							</para></listitem>
						</xsl:when>
						<xsl:otherwise>
							<term>
								<xsl:value-of select="@sig"/>
								<xsl:if test="@base">
									(inherited from <emphasis role="bold"><xsl:value-of select="@base"/></emphasis>)
								</xsl:if>
							</term>
							<listitem><para>
								<xsl:if test="@overridden = 'true'">
								Overridden.
								</xsl:if>
								Documentation not available.
							</para></listitem>
						</xsl:otherwise>
					</xsl:choose>
				</varlistentry>					
			</xsl:for-each>
		</variablelist>
	</xsl:template>
	
	<xsl:template match="method|property|field|event">
		<sect4 id="{@id}">
			<title>
				<xsl:value-of select="../../@name"/>.<xsl:value-of select="@name"/>
				<xsl:call-template name="MemberSwitch"/>
			</title>
			<xsl:apply-templates select="summary/*"/>
			<xsl:if test="name()='method'">
				<methodsynopsis>
					<modifier><xsl:value-of select="@modifiers"/></modifier>
					<type><xsl:value-of select="@returns"/></type>
					<methodname><xsl:value-of select="@name"/></methodname>
					<xsl:for-each select="param">
						<methodparam>
							<xsl:if test="@type">
								<type><xsl:value-of select="@type"/></type>
							</xsl:if>
							<parameter><xsl:value-of select="@name"/></parameter>
						</methodparam>
					</xsl:for-each>
				</methodsynopsis>
				<xsl:if test="param">
					<bridgehead>Parameters</bridgehead>
					<variablelist>
						<xsl:for-each select="param">
							<varlistentry>
								<term><emphasis><xsl:value-of select="@name"/></emphasis></term>
								<listitem><para><xsl:apply-templates/></para></listitem>
							</varlistentry>
						</xsl:for-each>
					</variablelist>
				</xsl:if>
			</xsl:if>
			<xsl:if test="name()='property' or name()='field' or name()='event'">
				<fieldsynopsis>
					<modifier><xsl:value-of select="@modifiers"/></modifier>
					<xsl:if test="@returns">
						<type><xsl:value-of select="@returns"/></type>
					</xsl:if>
					<varname><xsl:value-of select="@name"/></varname>
				</fieldsynopsis>
			</xsl:if>
			<xsl:if test="returns">
				<bridgehead>Return Value</bridgehead>
				<para><xsl:apply-templates select="returns/node()"/></para>
			</xsl:if>
			<xsl:if test="remarks">
				<bridgehead>Remarks</bridgehead>
				<xsl:apply-templates select="remarks/*"/>
			</xsl:if>
		</sect4>
	</xsl:template>
	
	<xsl:template match="baseRef"/>
	
	<xsl:template match="b">
		<emphasis role="bold"><xsl:apply-templates/></emphasis>
	</xsl:template>

	<xsl:template match="*">
		<xsl:copy>
			<xsl:apply-templates select="@*"/>
			<xsl:apply-templates/>
		</xsl:copy>
	</xsl:template>
</xsl:stylesheet>

