<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0"
    xmlns:tp="http://schemas.datacontract.org/2004/07/Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxs tp">
  <xsl:output method="html" indent="yes"/>
  <xsl:template match="/">
    <html>
      <body>
        <h1>TestRunDetails</h1>
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
        margin-top: 0;
        }
        .summary {background-color: #f4f4e1;font-family:monospace; }
        .row {
        border: 3px solid #ffffff;
        background-color: #f0f5fa;
        cursor:pointer;
        width:100%;
        }
        .innerRow{
        border: 2px solid #ffffff;
        padding-left:1%;
        margin-left:1%;
        background-color :#e9e1f4;
        cursor:pointer;
        }
        .pass { color: #0c0; }
        .fail { color: #c00; }
        .errorMessage{ color : brown; }
        .errorStackTrace{ color: brown; }
        .duration{float:right;padding-right:1%;}
      </style>
    </html>
  </xsl:template>

  <xsl:template match="/tp:TestRunDetails">
    <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary"/>
    <h2>Results</h2>
    <xsl:apply-templates select ="/tp:TestRunDetails/tp:Results"/>
  </xsl:template>

  <xsl:template match="/tp:TestRunDetails/tp:Results">
    <xsl:apply-templates select ="/tp:TestRunDetails/tp:Results/tp:TestResult"/>
  </xsl:template>

  <xsl:template match="tp:TestRunDetails/tp:Summary">
    <div class ="summary">
      <h2>Summary</h2>
      <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:TotalTests"/>
      <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:FailedTests"/>
      <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:PassedTests"/>
      <xsl:apply-templates select ="/tp:TestRunDetails/tp:Summary/tp:SkippedTests"/>
      <br/>
    </div>
  </xsl:template>

  <xsl:template match="tp:TestResult">
    <div class ="row" onclick="ToggleClass('{generate-id()}')">
      <div >
        <xsl:apply-templates select = "tp:resultOutcome" />
        <xsl:apply-templates select = "tp:FullyQualifiedName" />
        <div class="duration">
          <xsl:apply-templates select = "tp:Duration" />
        </div>
      </div>
      <xsl:if test ="tp:ErrorMessage!=''">
        <xsl:apply-templates select = "tp:ErrorMessage" />
      </xsl:if>
      <xsl:if test ="tp:ErrorStackTrace!=''">
        <xsl:apply-templates select = "tp:ErrorStackTrace" />
      </xsl:if>
      <xsl:if test ="tp:innerTestRunDetails!=''">
        <a Id="{generate-id()}" style="display:none;">
          <xsl:apply-templates select = "tp:innerTestRunDetails" />
        </a>
      </xsl:if>
      <br />
    </div>
  </xsl:template>

  <xsl:template match="tp:innerTestRunDetails/tp:TestResult">
    <div class="innerRow" onclick="ToggleClass('{generate-id()}')" >
      <div>
        <xsl:apply-templates select = "tp:resultOutcome" />
        <xsl:apply-templates select = "tp:FullyQualifiedName" />
        <div class="duration">
          <xsl:apply-templates select = "tp:Duration" />
        </div>
      </div>
      <xsl:if test ="tp:ErrorMessage!=''">
        <xsl:apply-templates select = "tp:ErrorMessage" />
      </xsl:if>
      <xsl:if test ="tp:ErrorStackTrace!=''">
        <xsl:apply-templates select = "tp:ErrorStackTrace" />
      </xsl:if>
      <xsl:if test ="tp:innerTestRunDetails!=''">
        <a Id="{generate-id()}" style="display:none;">
          <xsl:apply-templates select = "tp:innerTestRunDetails" />
        </a>
      </xsl:if>
      <br />
    </div>
  </xsl:template>

  <xsl:template match = "tp:ErrorMessage">
    &#160;&#160;
    ErrorMessage: <span class="errorMessage">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:ErrorStackTrace">
    &#160;&#160;
    ErrorStackTrace: <span class="errorStackTrace">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:FailedTests">
    <span>
      FailedTests:&#160;
    </span>
    <span class="failedTests">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:PassedTests">
    <span >
      PassedTests:&#160;
    </span>
    <span class="passedTests" >
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:SkippedTests">
    <span >
      SkippedTests:
    </span>
    <span class="skippedTests" >
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:TotalTests">
    <span >
      TotalTests:&#160;&#160;
    </span>
    <span class="totalTests">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:DisplayName">
    DisplayName:<span >
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:resultOutcome">
    <xsl:if test =" . = 'Passed' ">
      <span class="pass">
        &#x2714;
      </span>
    </xsl:if>
    <xsl:if test =" . = 'Failed'">
      <span class="fail">
        &#x2718;
      </span>
    </xsl:if>
  </xsl:template>

  <xsl:template match = "tp:Duration">
    <span >
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>
  <xsl:template match = "tp:FullyQualifiedName">
    <span >
      <xsl:value-of select = "." />
    </span>
  </xsl:template>
</xsl:stylesheet>
