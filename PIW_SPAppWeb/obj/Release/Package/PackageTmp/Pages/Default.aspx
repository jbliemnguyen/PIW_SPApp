<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="PIW_SPAppWeb.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Hello from PIW App</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <asp:TextBox runat="server" ID="txtTitle"></asp:TextBox>

        <br />
        <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Add Title to Announcement" />

    </div>
    </form>
</body>
</html>
