Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Net
Imports System.Threading
Imports Microsoft.WindowsAzure
Imports Microsoft.WindowsAzure.ServiceRuntime
Imports FibonacciLibrary

Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Auth
Imports Microsoft.WindowsAzure.Storage.Queue
Imports Microsoft.WindowsAzure.Storage.Table

Imports Microsoft.ApplicationServer.Caching

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class WorkerRole
    Inherits RoleEntryPoint

#Region "blackboard structures"
    Private Property sleepTime As Integer

    Private Shared _queue As CloudQueue
    Private Shared ReadOnly Property Queue As CloudQueue
        Get
            If _queue Is Nothing Then
                InitializeQueue()
            End If
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
    End Sub


    Private Shared _tempqueue As CloudQueue
    Private Shared ReadOnly Property TempQueue As CloudQueue
        Get
            If _tempqueue Is Nothing Then
                InitializeTempQueue()
            End If
            Return _tempqueue
        End Get
    End Property

    Private Shared Sub InitializeTempQueue()
        Dim account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"))
        Dim queueClient = account.CreateCloudQueueClient()
        ' Get a reference to a queue in this storage account.
        _tempqueue = queueClient.GetQueueReference("temp")
        ' Create the queue if it does not already exist.
        _tempqueue.CreateIfNotExists()
    End Sub


    Private Shared _cache As DataCache
    Private Shared ReadOnly Property Cache As DataCache
        Get
            If _cache Is Nothing Then
                InitializeCache()
            End If
            Return _cache
        End Get
    End Property
    Private Shared Sub InitializeCache()
        Dim factory As New DataCacheFactory
        _cache = factory.GetDefaultCache
    End Sub

    Private Sub UpdateNumberOfItemsProcessedInCache()
        Cache.Put("NumberOfItemsProcessedC", Convert.ToInt32(Cache.Get("NumberOfItemsProcessedC")) + 1)
    End Sub
    Private Sub ResetNumberOfItemsProcessedInCache(ByVal n As Integer)
        Cache.Put("NumberOfItemsProcessedC", n)
    End Sub

    Private Shared Property NumberOfItemsProcessed As Integer
        Get
            Dim retrieveOperation As TableOperation = TableOperation.Retrieve(Of FibonacciLibrary.BlackboardEntity)("BlackboardItem", "NumberOfItemsProcessed")
            Dim retrievedResult As TableResult = BlackboardTable.Execute(retrieveOperation)
            Dim retrievedEntity As FibonacciLibrary.BlackboardEntity = CType(retrievedResult.Result, FibonacciLibrary.BlackboardEntity)
            If retrievedEntity IsNot Nothing Then
                Return retrievedEntity.ItemValue
            Else
                ' means backboard is not initialized yet and no data is available
                Return 0
            End If
        End Get
        Set(value As Integer)
            Dim blackboardEntity As New FibonacciLibrary.BlackboardEntity("BlackboardItem", "NumberOfItemsProcessed")
            blackboardEntity.ItemValue = value
            Dim insertOperation As TableOperation = TableOperation.InsertOrReplace(blackboardEntity)
            BlackboardTable.Execute(insertOperation)
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

    Private Sub UpdateNumberOfScalingActionEnacted()
        ' update stats in the blackboard
        Dim retrieveOperation As TableOperation = TableOperation.Retrieve(Of FibonacciLibrary.BlackboardEntity)("BlackboardItem", "ScalingActionEnacted")
        Dim retrievedResult As TableResult = BlackboardTable.Execute(retrieveOperation)
        Dim retrievedEntity As FibonacciLibrary.BlackboardEntity = CType(retrievedResult.Result, FibonacciLibrary.BlackboardEntity)
        If retrievedEntity IsNot Nothing Then
            retrievedEntity.ItemValue += 1
        Else
            retrievedEntity = New FibonacciLibrary.BlackboardEntity("BlackboardItem", "ScalingActionEnacted")
            retrievedEntity.ItemValue = 1
        End If
        Dim insertOperation As TableOperation = TableOperation.InsertOrReplace(retrievedEntity)
        BlackboardTable.Execute(insertOperation)
    End Sub

#End Region

#Region "processing logic and blackboard update"
    Public Overrides Sub Run()

        Dim timeout As New TimeSpan(0, 0, 1)
        Dim message As CloudQueueMessage
        Dim reponse As Int64
        Dim input As Integer

        While (True)
            Thread.Sleep(sleepTime)
            Try
                message = Queue.GetMessage(timeout)
                If message IsNot Nothing Then
                    If Integer.TryParse(message.AsString, input) Then
                        If input >= 0 Then
                            reponse = Generator.Calculate(input)
                            Queue.DeleteMessage(message)
                            ' update structures for measuring stats with different accuracies
                            TempQueue.AddMessage(New CloudQueueMessage(reponse.ToString))
                            UpdateNumberOfItemsProcessedInCache()
                            NumberOfItemsProcessed += 1
                        ElseIf input = -1 Then
                            ' if input is equal -1 it means that we want to run new experiment and we already 
                            ' called ping in Fibonacci.svc to reset blackboard data
                            Queue.DeleteMessage(message)
                            ' resets the structures for new experiment
                            TempQueue.Clear()
                            ResetNumberOfItemsProcessedInCache(0)
                            NumberOfItemsProcessed = 0
                        End If
                    End If

                End If
            Catch ex As Exception
                Trace.TraceInformation("Something went wrong in the processor worker role!")
                Trace.TraceError(ex.Message)
            End Try

        End While

    End Sub

#End Region

#Region "role specific part"
    'Private Sub RoleEnvironmentChanged(ByVal sender As Object, ByVal e As RoleEnvironmentChangedEventArgs)
    '    ' update the enacted scaling action stats in the blackboard
    '    If e.Changes.Any(Function(change) TypeOf change Is RoleEnvironmentConfigurationSettingChange) Then
    '        UpdateNumberOfScalingActionEnacted()
    '    End If
    'End Sub

    Public Overrides Function OnStart() As Boolean

        ' Set the maximum number of concurrent connections 
        ' ServicePointManager.DefaultConnectionLimit = 12
        Trace.TraceInformation("Starting worker role")

        sleepTime = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("SleepTime"))

        ' update scale out stats on the blacboard
        UpdateNumberOfScalingActionEnacted()

        ' For information on handling configuration changes
        ' see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

        Return MyBase.OnStart()

    End Function

    Public Overrides Sub OnStop()
        ' put necessary commands before fully exiting the feedback loop

        ' update scaling in stats on the blackboard
        UpdateNumberOfScalingActionEnacted()

        MyBase.OnStop()
    End Sub

#End Region

End Class
