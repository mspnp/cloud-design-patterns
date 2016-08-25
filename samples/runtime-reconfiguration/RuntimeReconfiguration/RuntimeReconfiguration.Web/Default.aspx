<%-- 
Copyright (c) Microsoft. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
--%>
<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="RuntimeReconfiguration.Web.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <% var myComponent = RuntimeReconfiguration.Web.SomeRuntimeComponent.Instance; %>

            Current Value: <%= myComponent.CurrentValue %> <br/>
            Last application restart: <%= myComponent.LastRestart %><br/>

            <h4>Past values since last restart</h4>
            <ul>
            <% foreach (string value in myComponent.PastValues)
               { %>
                <li><%= value %></li>
            <% } %>
            </ul>
        </div>
    </form>
</body>
</html>
