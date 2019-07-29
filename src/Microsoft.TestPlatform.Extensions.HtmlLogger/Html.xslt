<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="2.0"
    
                xmlns:tp="http://schemas.datacontract.org/2004/07/Microsoft.VisualStudio.TestPlatform.Extensions.HtmlLogger"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxs tp"
>
  <xsl:output method="html" indent="yes"/>
  <xsl:template match="/">
    <html>
      <body>
        <h1>TestResults</h1>
        <xsl:apply-templates select ="/tp:TestResults"/>
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
        #testtable {
        border-collapse: collapse;
        }

        #testtable, th, td {
        border: 1px solid black;
        }
      </style>
    </html>
  </xsl:template>

  <xsl:template match="/tp:TestResults">
    <h2>Summary</h2>

    <xsl:apply-templates select ="/tp:TestResults/tp:Summary"/>

    <h2>Results</h2>
    <xsl:apply-templates select ="/tp:TestResults/tp:Results"/>

  </xsl:template>

  <xsl:template match="/tp:TestResults/tp:Results">
    <h3>TestResult</h3>
    <xsl:apply-templates select ="/tp:TestResults/tp:Results/tp:TestResult"/>
  </xsl:template>

  <xsl:template match="tp:TestResults/tp:Summary">
    <!--<table id="testtable">
      <tr>
        <th>Total Tests</th>
        <th>Failed Tests</th>
        <th>Passed Tests</th>
      </tr>
      <tr>
        <td>
          <xsl:value-of select="tp:TotalTests"/>
        </td>
        <td>
          <xsl:value-of select="tp:FailedTests"/>
        </td>
        <td>
          <xsl:value-of select="tp:PassedTests"/>
        </td>
      </tr>
     
    </table>-->
    <xsl:apply-templates select ="/tp:TestResults/tp:Summary/tp:TotalTests"/>
    <xsl:apply-templates select ="/tp:TestResults/tp:Summary/tp:FailedTests"/>
    <xsl:apply-templates select ="/tp:TestResults/tp:Summary/tp:PassedTests"/>

  </xsl:template>






  <xsl:template match="tp:TestResult">
    <div onclick="ToggleClass('{generate-id()}')" style="border:3px solid grey;cursor:pointer; width:100%;">

      <div>

        <xsl:apply-templates select = "tp:resultOutcome" />
        <xsl:apply-templates select = "tp:FullyQualifiedName" />
        <div  style="float:right;">
          <xsl:apply-templates select = "tp:Duration" />
        </div>

      </div>


      <!--<div style="width:60%;">
        <xsl:apply-templates select = "tp:FullyQualifiedName" />
      </div>-->

      <!--<xsl:apply-templates select = "tp:DisplayName" />-->
      <xsl:if test ="tp:ErrorMessage!=''">
        <xsl:apply-templates select = "tp:ErrorMessage" />
      </xsl:if>
      <xsl:if test ="tp:ErrorStackTrace!=''">
        <xsl:apply-templates select = "tp:ErrorStackTrace" />
      </xsl:if>


      <xsl:if test ="tp:innerTestResults!=''">
        <a Id="{generate-id()}" style="display:none;">
          <h3>innerTestResults:</h3>
          <xsl:apply-templates select = "tp:innerTestResults" />
        </a>
        <!--<br/>-->
      </xsl:if>
      <br />
    </div>
  </xsl:template>


  <xsl:template match="tp:innerTestResults/tp:TestResult">
    <div onclick="ToggleClass('{generate-id()}')" style="border:3px solid grey;cursor:pointer; width:100%;">

      <div>

        <xsl:apply-templates select = "tp:resultOutcome" />
        <xsl:apply-templates select = "tp:FullyQualifiedName" />
        <div  style="float:right;">
          <xsl:apply-templates select = "tp:Duration" />
        </div>

      </div>


      <!--<div style="width:60%;">
        <xsl:apply-templates select = "tp:FullyQualifiedName" />
      </div>-->

      <!--<xsl:apply-templates select = "tp:DisplayName" />-->
      <xsl:if test ="tp:ErrorMessage!=''">
        <xsl:apply-templates select = "tp:ErrorMessage" />
      </xsl:if>
      <xsl:if test ="tp:ErrorStackTrace!=''">
        <xsl:apply-templates select = "tp:ErrorStackTrace" />
      </xsl:if>


      <xsl:if test ="tp:innerTestResults!=''">
        <a Id="{generate-id()}" style="display:none;">
          <h3>innerTestResults:</h3>
          <xsl:apply-templates select = "tp:innerTestResults" />
        </a>
        <!--<br/>-->
      </xsl:if>
      <br />
    </div>
  </xsl:template>


  <xsl:template match = "tp:ErrorMessage">
    ErrorMessage: <span style = "color:brown;">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:ErrorStackTrace">
    ErrorStackTrace: <span style = "color:brown;">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:FailedTests">
    <span style = "width:10%">
      FailedTests:
    </span>
    <span style = "color:brown;padding-right:10px;">
      <xsl:value-of select = "." />
    </span>

    <br />
  </xsl:template>
  <xsl:template match = "tp:PassedTests">
    <span style = "width:10%">
      PassedTests:
    </span>
    <span style = "color:brown;padding-right:10px;">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>
  <xsl:template match = "tp:TotalTests">
    <span style = "width:10%">
      TotalTests:
    </span>
    <span style = "color:brown;padding-right:10px;">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:DisplayName">
    DisplayName:<span style = "color:brown;">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:resultOutcome">
    <!--<xsl:text>  &#x2714; </xsl:text>-->
    <xsl:if test ="tp:resultOutcome!='Passed'">
      <xsl:text>  &#x2714; </xsl:text>
    </xsl:if>
    <xsl:if test ="tp:resultOutcome!='Failed'">
      <xsl:apply-templates select = "pass" />
    </xsl:if>
    <span style = "color:brown;padding-right:10px;">
      <xsl:value-of select = "." />
    </span>

  </xsl:template>

  <xsl:template match = "pass">
    <span style = "color:brown;padding-right:10px;">
      &#x2714;
    </span>
  </xsl:template>

  <xsl:template match = "fail">
    <span style = "color:brown;padding-right:10px;">
      &#x2718;
    </span>
  </xsl:template>

  <xsl:template match = "tp:Duration">
    Duration:<span style = "color:brown;">
      <xsl:value-of select = "." />
    </span>
    <br />
  </xsl:template>

  <xsl:template match = "tp:FullyQualifiedName">
    <span style = "color:brown">
      <xsl:value-of select = "." />
    </span>
  </xsl:template>


  <!--<xsl:template match="@* | node()">
        <xsl:copy>
            <xsl:apply-templates select="@* | node()"/>
        </xsl:copy>
    </xsl:template>-->
</xsl:stylesheet>
