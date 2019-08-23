<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0"
    xmlns:tp="http://schemas.datacontract.org/2004/07/Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger.ObjectModel"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:a="http://schemas.microsoft.com/2003/10/Serialization/Arrays"            
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxs tp">
  <xsl:output method="html" indent="yes"/>
  <xsl:template match="/">
    <html>
      <body>
        <h1>Test run details</h1>
        <xsl:apply-templates select ="/tp:TestRunDetails"/>
      </body>
      <script language="javascript">
        function ToggleClass(id) {
        var elem = document.getElementById(id);
        if (elem.style.display == "none") {
        elem.style.display = "block";
        }
        else {
        elem.style.display = "none";
        }
        }
      </script>
      <style>
        body { font-family: Calibri, Verdana, Arial, sans-serif; background-color: White; color: Black; }
        h2 {
        margin-top: 15px;
        margin-bottom:10px;
        }
        .summary {font-family:monospace;
        display: -webkit-flex; /* Safari */
        -webkit-flex-wrap: wrap; /* Safari 6.1+ */
        display: flex;
        flex-wrap: wrap;
        }
        .row {
        border: 2px solid #ffffff;
        width:100%;
        cursor:pointer;
        background-color:#bcdaf7;
        }
        .listRow {
        border: 2px solid #ffffff;
        width:100%;
        cursor:pointer;
        background-color:#ffffff;
        }
        .innerRow{
        border: 2px solid #ffffff;
        padding-left:1%;
        margin-left:1%;
        background-color:#ffffff;
        }
        .block{
        width : 150px;
        }
        .leafDivision{
        border: 2px solid #ffffff;
        background-color:#e0f0ff;
        }
        .resultDivision{
        border: 2px solid #ffffff;
        padding-left:1%;
        margin-left:1%;
        background-color:#ffffff;
        }
        .pass { color: #0c0; }
        .fail { color: #c00; }
        .errorMessage { color : brown; }
        .errorStackTrace { color: brown; }
        .duration {float:right;padding-right:1%;}
        .totalTests { font-size : 30px}
        .testRunTime { font-size : 30px}
        .passPercentage { font-size : 30px}
        .errorInfo{
        margin-left:16px;
        }
      </style>
    </html>
  </xsl:template>

  <xsl:template match="/tp:TestRunDetails">
    <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary"/>
    <xsl:apply-templates select ="/tp:TestRunDetails/tp:ResultCollectionList"/>
  </xsl:template>

  <xsl:template match="tp:TestRunDetails/tp:Summary">
    <div class ="summary">
      <div class ="block"><xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:TotalTests"/></div>
      <div class ="block">
        <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:PassedTests"/>
        <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:FailedTests"/>
        <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:SkippedTests"/>
      </div>
      <div class ="block"><xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:PassPercentage"/></div>
      <div class ="block"><xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:TotalRunTime"/></div>
      <br/>
    </div>
  </xsl:template>

  <xsl:template match="/tp:TestRunDetails/tp:ResultCollectionList">
    <h2>Failed Results</h2><xsl:call-template name ="FailedResults"/>
    <h2>All Results</h2><xsl:call-template name ="Results"/>
  </xsl:template>

  <xsl:template name ="Results">
    <xsl:for-each select ="tp:TestResultCollection">
      <xsl:variable name="Source" select="tp:Id" />
      <xsl:if test="tp:ResultList!=''">
        <div class ="listRow" onclick="ToggleClass('{$Source}')"><xsl:value-of select = "tp:Source" /></div>
        <div class ="resultDivision" Id="{$Source}" style="display:none;">
          <xsl:for-each select ="tp:ResultList/tp:TestResult"><xsl:call-template name ="TestResult"/></xsl:for-each>
        </div>
      </xsl:if>
    </xsl:for-each>
  </xsl:template>

  <xsl:template name = "FailedResults">
    <xsl:for-each select ="tp:TestResultCollection">
      <xsl:variable name="Source" select="tp:Id" />
      <xsl:if test="tp:FailedResultList!=''">
        <div class ="listRow" onclick="ToggleClass('{concat($Source,'-failedResult')}')"><xsl:value-of select = "tp:Source" /> </div>
        <div class ="resultDivision" Id="{concat($Source,'-failedResult')}" style="display:block;">
          <xsl:for-each select ="tp:FailedResultList/tp:TestResult"><xsl:call-template name ="TestResult"/></xsl:for-each>
        </div>
      </xsl:if>
    </xsl:for-each>    
  </xsl:template>  
  
  <xsl:template name="TestResult" match="tp:TestResult">
    <xsl:variable name="TestResultId" select="tp:TestResultId" />
    
    <xsl:if test ="tp:InnerTestResults!=''">
      <div class ="row" onclick="ToggleClass('{concat($TestResultId,'-',name(..))}')"><xsl:call-template name = "Result" /></div>
      <a Id="{concat($TestResultId,'-',name(..))}" style="display:none;"><xsl:apply-templates select = "tp:InnerTestResults" /></a>
    </xsl:if> 
    
    <xsl:if test ="tp:InnerTestResults=''">
      <div class ="leafDivision"><xsl:call-template name = "Result" /></div>
    </xsl:if>   
    
  </xsl:template>
  
  <xsl:template match="tp:InnerTestResults">
    <div class ="innerRow"><xsl:apply-templates select = "tp:TestResult" /></div>
  </xsl:template>

<xsl:template name="Result" >
    <div>
      <xsl:apply-templates select = "tp:ResultOutcome"/>
      <xsl:apply-templates select = "tp:DisplayName" />
      <div class="duration"><xsl:apply-templates select = "tp:Duration" /></div>
    </div>
   <div class="errorInfo"> 
     <xsl:if test ="tp:ErrorMessage!=''"><xsl:apply-templates select = "tp:ErrorMessage" /></xsl:if>
     <xsl:if test ="tp:ErrorStackTrace!=''"><xsl:apply-templates select = "tp:ErrorStackTrace" /></xsl:if>
   </div>
  </xsl:template>
  
  <xsl:template match = "tp:ErrorMessage">
    Error: <span class="errorMessage"><xsl:value-of select = "." /></span><br />
  </xsl:template>

  <xsl:template match = "tp:ErrorStackTrace">
    Stack trace: <span class="errorStackTrace"><xsl:value-of select = "." /></span><br />
  </xsl:template>

  <xsl:template match = "tp:FailedTests">
    <span>Failed &#160;:&#160;</span><span class="failedTests"><xsl:value-of select = "." /></span><br />
  </xsl:template>

  <xsl:template match = "tp:PassedTests">
    <span>Passed &#160;:&#160;</span><span class="passedTests" ><xsl:value-of select = "." /></span><br />
  </xsl:template>

  <xsl:template match = "tp:SkippedTests">
    <span>Skipped :&#160;</span><span class="skippedTests" ><xsl:value-of select = "." /></span><br />
  </xsl:template>

  <xsl:template match = "tp:TotalTests">
    <span>Total tests</span><div  class="totalTests"><xsl:value-of select = "." /></div><br />
  </xsl:template>

  <xsl:template match = "tp:TotalRunTime">
    <span>Run duration</span><div  class="testRunTime" ><xsl:value-of select = "." /></div><br />
  </xsl:template>

  <xsl:template match = "tp:PassPercentage">
    <span>Pass percentage</span><div  class="passPercentage" ><xsl:value-of select = "." /> &#37;</div><br />
  </xsl:template>

  <xsl:template match = "tp:ResultOutcome">
    <xsl:if test =" . = 'Passed' "><span class="pass">&#x2714;</span></xsl:if>
    <xsl:if test =" . = 'Failed'"><span class="fail">&#x2718;</span></xsl:if>
    <xsl:if test =" . = 'Skipped'"><span class="skip">&#x2762;</span></xsl:if>
  </xsl:template>

  <xsl:template match = "tp:Duration">
    <span><xsl:value-of select = "." /></span><br />
  </xsl:template>
  
  <xsl:template match = "tp:DisplayName">
    <span>&#160;<xsl:value-of select = "." /></span>
  </xsl:template>
</xsl:stylesheet>
