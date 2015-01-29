Imports System.Configuration
Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Auth
Imports Microsoft.WindowsAzure.Storage.Table

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class ExperimentConsole
    Private dataDump As DataDump
    Private startTime As DateTime
    Private finishTime As DateTime
    Private loadlib As LoadLibrary

    Private Shared _table As CloudTable
    Private Shared ReadOnly Property BlackboardTable As CloudTable
        Get
            If _table Is Nothing Then InitializeTable()
            Return _table
        End Get
    End Property
    Private Shared Sub InitializeTable()
        Dim account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings("DataConnectionString"))
        Dim tableClient = account.CreateCloudTableClient()
        _table = tableClient.GetTableReference("blackboard")
        _table.CreateIfNotExists()

        Dim blackboardEntity As New FibonacciLibrary.BlackboardEntity("BlackboardItem", "ExecutionMode")
        blackboardEntity.ItemValue = FibonacciLibrary.BlackboardEntity.RoleMode.Idle
        Dim insertOperation As TableOperation = TableOperation.InsertOrReplace(blackboardEntity)
        _table.Execute(insertOperation)
    End Sub

    Private Sub SwitchRoleMode(ByVal newMode As FibonacciLibrary.BlackboardEntity.RoleMode)
        ' update stats in the blackboard
        Dim blackboardEntity As New FibonacciLibrary.BlackboardEntity("BlackboardItem", "ExecutionMode")
        blackboardEntity.ItemValue = newMode
        Dim insertOperation As TableOperation = TableOperation.InsertOrReplace(blackboardEntity)
        BlackboardTable.Execute(insertOperation)
    End Sub

    Private Sub DumpAll_Click(sender As Object, e As EventArgs) Handles DumpAll.Click
        dataDump.DumpAllExperimentsData()
    End Sub

    Private Sub Start_Click(sender As Object, e As EventArgs) Handles Start.Click
        startTime = DateTime.Now
        SwitchRoleMode(FibonacciLibrary.BlackboardEntity.RoleMode.ExperimentRunning)
    End Sub

    Private Sub Finish_Click(sender As Object, e As EventArgs) Handles Finish.Click
        finishTime = DateTime.Now
        SwitchRoleMode(FibonacciLibrary.BlackboardEntity.RoleMode.Idle)
        dataDump.DumpExperimentData(startTime, finishTime)
    End Sub

    Private Sub PushLoad(ByVal load() As Object)
        loadlib.constantLoad(Convert.ToInt32(load(0)), Convert.ToInt32(load(1)))
    End Sub


    Private Sub Generate_Click(sender As Object, e As EventArgs) Handles Generate.Click
        'Dim t1 As New System.Threading.Thread(AddressOf PushLoad)
        'Dim load(1) As Object
        'load(0) = "100"
        'load(1) = "4"
        't1.Start(load)
        'Dim t2 As New System.Threading.Thread(AddressOf PushLoad)
        'load(0) = "100"
        'load(1) = "5"
        't2.Start(load)
        'loadlib.constantLoad(100000, 4)
        loadlib.uniformLoad(86400000)
    End Sub

    Private Sub ExperimentConsole_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'SwitchRoleMode(FibonacciLibrary.BlackboardEntity.RoleMode.Idle)
        dataDump = New DataDump
        loadlib = New LoadLibrary
    End Sub
End Class