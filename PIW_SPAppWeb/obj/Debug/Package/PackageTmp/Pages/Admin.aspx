<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Admin.aspx.cs" Inherits="PIW_SPAppWeb.Default" %>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>

<body>
    <form id="form1" runat="server">
    <div>
    <asp:TextBox runat="server" ID="txtTitle"></asp:TextBox>

        <br />
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Add Title to Announcement" />
        <asp:Button ID="btnRegisterRER" runat="server" OnClick="btnRegisterRER_Click" Text="Register Remote Event Receiver for PIW Documents" BorderStyle="Ridge" />

        <br />
        <asp:Button ID="btnRemoveRER" runat="server" OnClick="btnRemoveRER_Click" Text="Remove Remote Event Receiver for PIW Documents" BorderStyle="Ridge" />

        <br />
        <br />
        <a href="PeoplePicker.aspx">Test people picker</a>
    </div>
    </form>
</body>

</html>
