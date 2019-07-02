<%@ Page Language="C#" %>
<%
foreach (string var in Request.ServerVariables)
{
  Response.Write(var + " " + Request[var] + "<br>");
}
%>


1)	Granting identity of your ASP.NET application a read access to the encryption key that is used to encrypt and decrypt the encrypted sections.