<%-- 
==============================================================================================================
Microsoft patterns & practices
Cloud Design Patterns project
==============================================================================================================
©2013 Microsoft. All rights reserved. 
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance 
with the License. You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is 
distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
See the License for the specific language governing permissions and limitations under the License.
==============================================================================================================
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
