<%@ Page Language="C#" %>
<%@ Assembly Name="Ict.Common" %>
<%@ Assembly Name="Ict.Common.IO" %>
<%@ Import Namespace="Ict.Common" %>
<%@ Import Namespace="Ict.Common.IO" %>

<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Web" %>

<%
new TAppSettingsManager("web.config");
new TLogging(TAppSettingsManager.GetValueStatic("Server.LogFile"));
try
{
    if (HttpContext.Current.Request.Params["pdf-id"] != null && HttpContext.Current.Request.Params["pdf-id"].Length > 0)
    {
        string LinkFilename = TAppSettingsManager.GetValueStatic("Server.PathData") + Path.DirectorySeparatorChar + "downloads" + Path.DirectorySeparatorChar +  Path.GetFileName(HttpContext.Current.Request.Params["pdf-id"]) + ".txt";
        
        StreamReader rw = new StreamReader(LinkFilename);
        string pdfFileName = TAppSettingsManager.GetValueStatic("Server.PathData") + Path.DirectorySeparatorChar + rw.ReadLine() + Path.DirectorySeparatorChar + rw.ReadLine();
        
        Response.Buffer = true;
        Response.Clear();
        Response.ClearContent(); 
        Response.ClearHeaders();         
        Response.ContentType = "application/pdf";
        Response.AppendHeader("Content-Disposition","attachment; filename=application.pdf");
        TLogging.Log(pdfFileName);
        Response.TransmitFile( pdfFileName );
        // comment Response.End() to avoid System.Threading.ThreadAbortException
        // Response.End();
    }
}
catch (Exception e)
{
    TLogging.Log(e.ToString() + ": " + e.Message);
    TLogging.Log(e.StackTrace);
    return;
}    
%>

