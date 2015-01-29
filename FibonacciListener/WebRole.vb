Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports Microsoft.WindowsAzure
Imports Microsoft.WindowsAzure.ServiceRuntime

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class WebRole
    Inherits RoleEntryPoint

    Public Overrides Function OnStart() As Boolean
        Trace.TraceInformation("Starting web role")
        ' initialize blackboard table and the queue by pinging the WCF service
        Dim svc As New Fibonacci
        svc.Ping()

        ' For information on handling configuration changes
        ' see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

        Return MyBase.OnStart()

    End Function

    Public Overrides Sub OnStop()

    End Sub

End Class

