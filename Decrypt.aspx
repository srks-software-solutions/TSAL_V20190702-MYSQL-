<%@ Page Language="C#" %>
<%@ Import Namespace="System.Configuration" %>
<%@ Import Namespace="System.Web.Configuration" %>
<script runat="server">

public void Page_Load()
{
  ConnectionStringsGrid.DataSource = ConfigurationManager.ConnectionStrings;
  ConnectionStringsGrid.DataBind();

  Configuration config = WebConfigurationManager.OpenWebConfiguration(Request.ApplicationPath);
  MachineKeySection key = 
    (MachineKeySection)config.GetSection("system.web/machineKey");
  DecryptionKey.Text = key.DecryptionKey;
  ValidationKey.Text = key.ValidationKey;
}

</script>
<html>

<body>

<form runat="server">

  <asp:GridView runat="server" CellPadding="4" id="ConnectionStringsGrid" />
  <P>
  MachineKey.DecryptionKey = <asp:Label runat="Server" id="DecryptionKey" /><BR>
  MachineKey.ValidationKey = <asp:Label runat="Server" id="ValidationKey" />

</form>

</body>
</html>