Imports Microsoft.WindowsAzure
Imports Microsoft.WindowsAzure.ServiceRuntime
Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Auth
Imports Microsoft.WindowsAzure.Storage.Queue
Imports Microsoft.WindowsAzure.Storage.Table

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class Fibonacci
    Implements IFibonacci

    Private Shared _queue As CloudQueue

    Private Shared ReadOnly Property Queue As CloudQueue
        Get
            If _queue Is Nothing Then InitializeQueue()
            Return _queue
        End Get
    End Property

    Private Shared Sub InitializeQueue()
        Dim account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"))
        Dim queueClient = account.CreateCloudQueueClient()
        ' Get a reference to a queue in this storage account.
        _queue = queueClient.GetQueueReference("fibonacci")
        ' Create the queue if it does not already exist.
        _queue.CreateIfNotExists()
        _queue.Clear()
    End Sub

    Private Shared _number_of_items As Integer
    Private Shared Property NumberOfItemsInserted As Integer
        Get
            Return _number_of_items
        End Get
        Set(value As Integer)
            _number_of_items = value
        End Set
    End Property


    Private Shared _table As CloudTable

    Private Shared ReadOnly Property BlackboardTable As CloudTable
        Get
            If _table Is Nothing Then InitializeTable()
            Return _table
        End Get
    End Property

    Private Shared Sub InitializeTable()
        Dim account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"))
        Dim tableClient = account.CreateCloudTableClient()
        _table = tableClient.GetTableReference("blackboard")
        _table.CreateIfNotExists()
    End Sub

    Private Sub UpdateNumberOfItemsInserted()
        ' update stats in the blackboard
        Dim blackboardEntity As New FibonacciLibrary.BlackboardEntity("BlackboardItem", "NumberOfItemsInserted")
        blackboardEntity.ItemValue = NumberOfItemsInserted
        Dim insertOperation As TableOperation = TableOperation.InsertOrReplace(blackboardEntity)
        BlackboardTable.Execute(insertOperation)
    End Sub

    Public Sub BeginCalculate(ByVal n As Integer) _
    Implements IFibonacci.BeginCalculate
        Queue.AddMessage(New CloudQueueMessage(n.ToString))
        If n >= 0 Then
            NumberOfItemsInserted += 1
        ElseIf n = -1 Then
            ' this means we want to run new experiment
            NumberOfItemsInserted = 0
        End If
        UpdateNumberOfItemsInserted()
    End Sub

    Public Function Calculate(ByVal n As Integer) As Long _
    Implements IFibonacci.Calculate
        Return FibonacciLibrary.Generator.Calculate(n)
    End Function

    Public Function Ping() As Boolean _
    Implements IFibonacci.Ping
        ' initilize queue when the experiment is started
        InitializeQueue()
        BeginCalculate(-1) ' see FibonacciProcessor worker role in Run() procedure
        Return True
    End Function
End Class