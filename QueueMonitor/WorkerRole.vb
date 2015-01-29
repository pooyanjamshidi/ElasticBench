Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Net
Imports System.Threading
Imports Microsoft.WindowsAzure
Imports Microsoft.WindowsAzure.Diagnostics
Imports Microsoft.WindowsAzure.ServiceRuntime
Imports System.Security.Cryptography.X509Certificates
Imports System.IO
Imports System.Text
Imports Microsoft.WindowsAzure.Management
Imports Microsoft.WindowsAzure.Management.Compute
Imports Microsoft.WindowsAzure.Management.Compute.Models
Imports Microsoft.WindowsAzure.Management.Monitoring

Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Auth
Imports Microsoft.WindowsAzure.Storage.Queue
Imports Microsoft.WindowsAzure.Storage.Table

Imports Microsoft.ApplicationServer.Caching

Imports RobusT2ScaleNative
Imports MathWorks.MATLAB.NET.Arrays
Imports MathWorks.MATLAB.NET.Utility
Imports Learning


''' <summary>
''' Authors: Pooyan Jamshidi (pooyan.jamshidi@gmail.com)
''' This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.
''' </summary>
''' <remarks></remarks>

Public Class WorkerRole
    Inherits RoleEntryPoint

#Region "required variables here"

    Private itemCountQueue As Analytics
    Private processedCountQueue As Analytics
    Private requestCountQueue As Analytics
    Private elapsedTimeQueue As Analytics
    Private responseTimeQueue As Analytics

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

    Private Function RetrieveNumberOfRequestsProcessedFromCache() As Integer
        Return Convert.ToInt32(Cache.Get("NumberOfItemsProcessedC"))
    End Function


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
    Private Function RetrieveNumberOfRequests() As Integer
        Dim retrieveOperation As TableOperation = TableOperation.Retrieve(Of FibonacciLibrary.BlackboardEntity)("BlackboardItem", "NumberOfItemsInserted")
        Dim retrievedResult As TableResult = BlackboardTable.Execute(retrieveOperation)
        Dim retrievedEntity As FibonacciLibrary.BlackboardEntity = CType(retrievedResult.Result, FibonacciLibrary.BlackboardEntity)
        If retrievedEntity IsNot Nothing Then
            Return retrievedEntity.ItemValue
        Else
            ' means blackboard is not initialized yet and no data is available
            Return -1
        End If
    End Function

    Private Function RetrieveNumberOfRequestsProcessed() As Integer
        Dim retrieveOperation As TableOperation = TableOperation.Retrieve(Of FibonacciLibrary.BlackboardEntity)("BlackboardItem", "NumberOfItemsProcessed")
        Dim retrievedResult As TableResult = BlackboardTable.Execute(retrieveOperation)
        Dim retrievedEntity As FibonacciLibrary.BlackboardEntity = CType(retrievedResult.Result, FibonacciLibrary.BlackboardEntity)
        If retrievedEntity IsNot Nothing Then
            Return retrievedEntity.ItemValue
        Else
            ' means backboard is not initialized yet and no data is available
            Return -1
        End If
    End Function

    Private Function RetrieveNumberOfScalingActionsEnacted() As Integer
        ' update stats in the blackboard
        Dim numberOfEnacteActions As Integer
        Dim retrieveOperation As TableOperation = TableOperation.Retrieve(Of FibonacciLibrary.BlackboardEntity)("BlackboardItem", "ScalingActionEnacted")
        Dim retrievedResult As TableResult = BlackboardTable.Execute(retrieveOperation)
        Dim retrievedEntity As FibonacciLibrary.BlackboardEntity = CType(retrievedResult.Result, FibonacciLibrary.BlackboardEntity)
        If retrievedEntity IsNot Nothing Then
            If retrievedEntity.ItemValue > 0 Then
                numberOfEnacteActions = retrievedEntity.ItemValue
                ' reset value
                retrievedEntity.ItemValue = 0
            End If
            Dim insertOperation As TableOperation = TableOperation.InsertOrReplace(retrievedEntity)
            BlackboardTable.Execute(insertOperation)
            Return numberOfEnacteActions
        End If
        Return 0
    End Function

    Private Function RetrieveRoleMode() As FibonacciLibrary.BlackboardEntity.RoleMode
        Dim retrieveOperation As TableOperation = TableOperation.Retrieve(Of FibonacciLibrary.BlackboardEntity)("BlackboardItem", "ExecutionMode")
        Dim retrievedResult As TableResult = BlackboardTable.Execute(retrieveOperation)
        Dim retrievedEntity As FibonacciLibrary.BlackboardEntity = CType(retrievedResult.Result, FibonacciLibrary.BlackboardEntity)
        If retrievedEntity IsNot Nothing Then
            Return retrievedEntity.ItemValue
        Else
            ' means backboard is not initialized yet so the default mode is returned
            Return FibonacciLibrary.BlackboardEntity.RoleMode.Idle
        End If
    End Function


    Private Shared _resulttable As CloudTable
    Private Shared ReadOnly Property ResultTable As CloudTable
        Get
            If _resulttable Is Nothing Then InitializeResultTable()
            Return _resulttable
        End Get
    End Property
    Private Shared Sub InitializeResultTable()
        Dim account = CloudStorageAccount.Parse(RoleEnvironment.GetConfigurationSettingValue("DataConnectionString"))
        Dim tableClient = account.CreateCloudTableClient()
        _resulttable = tableClient.GetTableReference("result")
        _resulttable.CreateIfNotExists()
    End Sub

    Private Property scaleWhileTransitioning As Boolean
    Private Property successfullyIssued As Boolean = False
    Private Property Latency As Integer
    Private Property minNode As Integer
    Private Property maxNode As Integer
    Private Property maxWorkload As Double
    Private Property SLOrt As Double

    Private _WindowLength As Integer
    Private ReadOnly Property WindowLength As Integer
        Get
            If _WindowLength = 0 Then
                Dim configSetting = RoleEnvironment.GetConfigurationSettingValue("WindowLength")
                Integer.TryParse(configSetting, _WindowLength)
            End If
            Return _WindowLength
        End Get
    End Property

#End Region

#Region "utility functions for encoding/decoding, data scaling"

    Public Function EncodeTo64(ByVal toEncode As String) As String
        Dim bytes As Byte()
        bytes = ASCIIEncoding.ASCII.GetBytes(toEncode)
        Return System.Convert.ToBase64String(bytes)
    End Function

    Public Function DecodeFromBase64String(ByVal base64Value As String) As String
        Dim bytes As Byte() = Convert.FromBase64String(base64Value)
        Dim value As String = System.Text.Encoding.UTF8.GetString(bytes)
        Return value
    End Function

    Public Function ScaleData(ByVal datain As Double, ByVal minval As Double, ByVal maxval As Double, ByVal range As Double) As Double
        Dim dataout As Double
        If datain <= range Then
            dataout = datain
            dataout = (dataout / range) * (maxval - minval)
            dataout = dataout + minval
        Else
            dataout = maxval
        End If
        Return dataout
    End Function

#End Region

#Region "policy enforcer"

    ' policy enforcement logic goes here
    ' here we use utility functions we wrote for Azure platform to enfore policies such as constraints as lowest number of worker roles, highest number of wroker roles, etc
    ' note that all the logic here should be platform independent and leave the code be reusable for other platforms such as AWS as well

    Private Function EnforcePolicy(ByVal number_of_nodes_to_be_changed As Integer) As Boolean
        Dim managementCert As X509Certificate2 = GetCertificate()
        Dim creds As SubscriptionCloudCredentials = New CertificateCloudCredentials(SUBSCRIPTION_ID, managementCert)
        Dim computeManagementClient As ComputeManagementClient = CloudContext.Clients.CreateComputeManagementClient(creds)

        Dim detailed = computeManagementClient.HostedServices.GetDetailed(SERVICE_NAME)
        Dim deployment = detailed.Deployments.First()

        Dim configFile As String = deployment.Configuration

        Dim current_nodes = GetInstanceCount(configFile)
        Dim change_is_ok As Boolean = True


        ' check whether changing the number of nodes violate the scaling constraints
        If current_nodes + number_of_nodes_to_be_changed < minNode Or current_nodes + number_of_nodes_to_be_changed > maxNode Then
            change_is_ok = False
        End If

        ' check whether the cloud service deployment is in the "Running" state otherwise it 
        ' will be in the trnasitioning states and the new scaling action should be ignored 

        If deployment.Status <> Compute.Models.DeploymentStatus.Running And scaleWhileTransitioning = False Then
            change_is_ok = False
        End If

        ' check whether the deployment is locked
        If deployment.Locked = True Then
            change_is_ok = False
        End If

        ' more policies can be harcoded here


        Return change_is_ok
    End Function

#End Region

#Region "actuator, platform (Azure) specific code"
    ' actuator code goes here 
    ' the logic here are specific to Azure platform

    Private _thumbprint As String
    Private ReadOnly Property ThumbPrint As String
        Get
            ' thumbprint can be found in the certificate details
            ' in certmgr.msc
            ' this cetificate should be export as something.cer then it should have been uploaded to the azure management certificate on https://manage.windowsazure.com
            ' for a more detailed description of how to generate certificate follow this:
            ' (1) Load the IIS 7 management console.
            ' (2) Double Click Server Certificates in the IIS Section in the main panel.
            ' (3) Create Self-Signed Certificate… in the Actions panel.
            ' (4) Open Certificate Manager (Start->Run->certmgr.msc)
            ' (5) Open Trusted Root Certification Authorities, then Certificates.
            ' (6) Look for your certificate (Tip: Look in the Friendly Name column).
            ' (7) Right Click your certificate, then choose All Tasks, then Export…
            ' (8) In the Wizard, choose No, do not export the private key, then choose the DER file format.
            ' (9) Give your cert a name. (remember to call it something.cer).
            ' (10) Navigate to the Windows Azure Portal – http://windows.azure.com
            ' (11) Browse to the certificate file you created earlier and upload it.
            If _thumbprint Is Nothing Then
                _thumbprint = RoleEnvironment.GetConfigurationSettingValue("CertificateThumbprint")
            End If
            Return _thumbprint
        End Get
    End Property

    ''' <summary>
    '''  This procedure gets an integer and alter the number of nodes in the udnerlying deployment 
    ''' </summary>
    ''' <param name="number_of_changing_node">if positive-> scale out; if negative-> scale in</param>
    ''' <remarks> </remarks>
    Private Sub ChangeNumberOfNodes(ByVal number_of_changing_node As Integer)

        Dim managementCert As X509Certificate2 = GetCertificate()
        Dim creds As SubscriptionCloudCredentials = New CertificateCloudCredentials(SUBSCRIPTION_ID, managementCert)
        Dim computeManagementClient As ComputeManagementClient = CloudContext.Clients.CreateComputeManagementClient(creds)

        Dim detailed = computeManagementClient.HostedServices.GetDetailed(SERVICE_NAME)
        Dim deployment = detailed.Deployments.First()

        Dim configFile As String = deployment.Configuration

        Dim current_nodes = GetInstanceCount(configFile)
        EnactChange(current_nodes + number_of_changing_node)
    End Sub


    Private sc As XNamespace = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration"
    Private Function GetInstanceCount(ByVal config As XElement) As Int32
        Dim instanceElement As XElement =
            (From s In config.Elements(sc + "Role")
             Where s.Attribute("name").Value = ROLE_NAME
             Select s.Element(sc + "Instances")).First()

        Dim instanceCount As Int32 = Convert.ToInt32(instanceElement.Attribute("count").Value)
        Return instanceCount
    End Function

    Private Function GetReadyInstanceCount() As Int32
        Dim managementCert As X509Certificate2 = GetCertificate()
        Dim creds As SubscriptionCloudCredentials = New CertificateCloudCredentials(SUBSCRIPTION_ID, managementCert)
        Dim computeManagementClient As ComputeManagementClient = CloudContext.Clients.CreateComputeManagementClient(creds)
        Dim detailed = computeManagementClient.HostedServices.GetDetailed(SERVICE_NAME)
        Dim deployment = detailed.Deployments.First()
        Dim configFile As String = deployment.Configuration
        Dim current_nodes = GetInstanceCount(configFile)
        Dim readyCount As Integer = 0
        For i = 0 To current_nodes - 1
            If deployment.RoleInstances(i).InstanceStatus = "ReadyRole" Then 'Indicates that a role instance has started and is ready to be used. http://msdn.microsoft.com/en-us/library/azure/ee460804.aspx
                readyCount += 1
            End If
        Next
        Return readyCount
    End Function

    Private Function GetInstanceCount(ByVal config As String) As Int32
        Dim startIndex As Integer
        Dim endIndex As Integer

        With config
            startIndex = .IndexOf(ROLE_NAME)
            startIndex = .IndexOf(INSTANCE_COUNT, startIndex)
            startIndex += INSTANCE_COUNT.Length + 1
            endIndex = .IndexOf("""", startIndex)

        End With

        Return config.Substring(startIndex, endIndex - startIndex)
    End Function

    Private Sub EnactChange(ByVal nodeCount As Integer)
        Try
            Dim managementCert As X509Certificate2 = GetCertificate()
            Dim creds As SubscriptionCloudCredentials = New CertificateCloudCredentials(SUBSCRIPTION_ID, managementCert)
            Dim computeManagementClient As ComputeManagementClient = CloudContext.Clients.CreateComputeManagementClient(creds)

            Dim detailed = computeManagementClient.HostedServices.GetDetailed(SERVICE_NAME)
            Dim deployment = detailed.Deployments.First()

            Dim configFile As String = deployment.Configuration

            Dim config As String = GetConfig(configFile, nodeCount)
            Dim base64Config As String = EncodeTo64(config)
            Dim request As HttpWebRequest
            request = CreateAzureConfigHttpRequest(base64Config)
            request.GetResponse()
            successfullyIssued = True
        Catch e As WebException
            successfullyIssued = False
        End Try

    End Sub

    Private Const ROLE_NAME As String = "FibonacciProcessor"
    Private Const INSTANCE_COUNT As String = "Instances count="
    Private Function GetConfig(ByVal config As String, ByVal nodeCount As Integer) As String

        ' find the correct role and the instance count
        Dim startIndex As Integer
        Dim endIndex As Integer
        Dim output As String

        With config
            startIndex = .IndexOf(ROLE_NAME)
            startIndex = .IndexOf(INSTANCE_COUNT, startIndex)
            startIndex += INSTANCE_COUNT.Length + 1

            endIndex = .IndexOf("""", startIndex)

            output = .Substring(0, startIndex)
            output &= nodeCount.ToString
            output &= .Substring(endIndex)
        End With

        Return output
    End Function

    Private SUBSCRIPTION_ID As String
    Private SERVICE_NAME As String

    Private Function CreateAzureConfigHttpRequest(ByVal base64Config As String) As HttpWebRequest

        'first step toward requesting a RESTFull service is to get appropriate certificate then issue the request
        Dim cert As X509Certificate2 = GetCertificate()

        ' here I should subscription id and service name in the config file and retirve the value from there!
        Dim uri As String
        uri = "https://management.core.windows.net/"
        uri &= SUBSCRIPTION_ID + "/services/hostedservices/"
        uri &= SERVICE_NAME + "/deploymentslots/production/"
        uri &= "?comp=config"

        Dim request As HttpWebRequest
        request = HttpWebRequest.Create(uri)
        request.Timeout = 5000
        request.ReadWriteTimeout = request.Timeout * 100
        request.Method = "POST"
        request.ClientCertificates.Add(cert)
        request.Headers.Add("x-ms-version", "2011-02-25")
        request.ContentType = "application/xml"

        Dim content As XDocument = GetConfigString(base64Config)
        Using requestStream As Stream = request.GetRequestStream()
            Using StreamWriter As StreamWriter = New StreamWriter(requestStream, System.Text.UTF8Encoding.UTF8)
                content.Save(StreamWriter, SaveOptions.DisableFormatting)
            End Using
        End Using

        Return request

    End Function


    Private Function GetServiceDeploymentStatus(ByVal base64Config As String) As String

        'first step toward requesting a RESTFull service is to get appropriate certificate then issue the request
        Dim cert As X509Certificate2 = GetCertificate()

        ' here I should subscription id and service name in the config file and retirve the value from there!
        Dim uri As String
        uri = "https://management.core.windows.net/"
        uri &= SUBSCRIPTION_ID + "/services/hostedservices/"
        uri &= SERVICE_NAME
        uri &= "?embed-detail=true"

        Dim request As HttpWebRequest
        request = HttpWebRequest.Create(uri)
        request.Timeout = 5000
        request.ReadWriteTimeout = request.Timeout * 100
        request.Method = "POST"
        request.ClientCertificates.Add(cert)
        request.Headers.Add("x-ms-version", "2011-02-25")
        request.ContentType = "application/xml"

        Dim content As XDocument = GetConfigString(base64Config)
        Using requestStream As Stream = request.GetRequestStream()
            Using StreamWriter As StreamWriter = New StreamWriter(requestStream, System.Text.UTF8Encoding.UTF8)
                content.Save(StreamWriter, SaveOptions.DisableFormatting)
            End Using
        End Using

        Dim response As System.IO.Stream
        response = request.GetResponse().GetResponseStream()
        Dim xmlofResponse = New StreamReader(response).ReadToEnd()

        Dim el As XElement = XElement.Parse(xmlofResponse)
        Dim ns As XNamespace = "http://schemas.microsoft.com/windowsazure"

        Dim instanceElement As XElement =
            (From s In el.Elements(ns + "Deployments")
             Select s.Element(ns + "Deployment"))

        Dim deploymentStatus As String = instanceElement.Attribute("Status").Value

        Return deploymentStatus
    End Function

    Private wa As XNamespace = "http://schemas.microsoft.com/windowsazure"
    Private Function GetConfigString(ByVal base64Config As String) As XDocument

        Dim xConfiguration As XElement = New XElement(wa + "Configuration", base64Config)
        Dim xChangeConfiguration As XElement = New XElement(wa + "ChangeConfiguration", xConfiguration)
        Dim payload As XDocument = New XDocument()
        payload.Add(xChangeConfiguration)
        payload.Declaration = New XDeclaration("1.0", "utf-8", "no")
        Return payload
    End Function

    Private Function GetCertificate() As X509Certificate2

        'Dim locations As New List(Of StoreLocation)

        For Each storeLocation As StoreLocation In CType([Enum].GetValues(GetType(StoreLocation)), StoreLocation())
            ' For Each storeName As StoreName In CType([Enum].GetValues(GetType(StoreName)), StoreName())
            Dim store As New X509Store(StoreName.My, storeLocation)
            Try
                store.Open(OpenFlags.ReadOnly Or OpenFlags.OpenExistingOnly)
                Dim cert = store.Certificates.Find(X509FindType.FindByThumbprint, ThumbPrint, False)
                ' return the certificate if it could locate a unique certificate
                If cert.Count = 1 Then
                    Return cert(0)
                End If
            Finally
                store.Close()
            End Try
        Next

        Throw New ArgumentException(String.Format("A Certificate with Thumbprint '{0}' could not be located.", ThumbPrint))

        'Dim store As New X509Store(StoreName.My, StoreLocation.LocalMachine)
        'store.Open(OpenFlags.ReadOnly Or OpenFlags.OpenExistingOnly)
        'Dim cert = store.Certificates.Find(X509FindType.FindByThumbprint, ThumbPrint, True)
        '' return the certificate if it could locate a unique certificate
        'Return If(cert.Count = 1, cert(0), Nothing)
    End Function

#End Region

#Region "MAPE loop"
    Public Overrides Sub Run()

        Dim processedCount As Integer = 0
        Dim preProcessedCount As Integer = 0

        Dim processedCountC As Integer = 0
        Dim preProcessedCountC As Integer = 0

        Dim processedCountT As Integer = 0
        Dim preProcessedCountT As Integer = 0

        Dim requestCount As Integer = 0
        Dim preRequestCount As Integer = 0
        Dim elapsedTimeInFeedbackLoop As Long
        Dim averageRequestPerSec As Double

        Dim loopwatch As New Stopwatch
        Dim watch As New Stopwatch

        Dim controller As New RobusT2Scale ' Auto-scaler controller

        Dim myFQL As New FQL  ' Fuzzy Q Learner class
        Dim QMW As MWNumericArray  ' temporary variable for retrieving data from matlab function
        Dim LEARNMW As MWNumericArray
        Dim Q As Double(,)  ' Q look up table
        Dim LEARN As Double(,) ' LEARN maintain the number of updates for each cell in the Q table
        Dim currentState As Double()
        Dim currentAction As Integer
        Dim currentais As Double(,)
        Dim result As MWArray()
        Dim epoch As Integer = 0
        Dim epsilon As Double = 1
        Dim canUpdate As Boolean = False

        Dim array2string As New FibonacciLibrary.DoublesArrayEntity

        '0. initializing knowledge base
        myFQL.init_knowledge_base()

        Trace.TraceInformation("QueueMonitor entry point called.", "Information")

        ' This while loop is the main feedback control loop, MAPE loop
        ' In this feedback control loop we do Monitoring, Analysis, Plan the scaling, and Execute the change in the resources
        While (True)

            Try

                If RetrieveRoleMode() = FibonacciLibrary.BlackboardEntity.RoleMode.ExperimentRunning Then

                    ' initiaqlizing and resetting scaling decision
                    Dim ScalingDecision As Integer = 0

                    watch.Start()
                    '--------------------------------------------------------------------------------------------------------------------
                    ' put probeing data to the queues for later metric measurements

                    Queue.FetchAttributes()
                    Dim messageCount = Queue.ApproximateMessageCount
                    If messageCount.HasValue Then
                        itemCountQueue.Enqueue(messageCount.Value)
                    End If


                    ' measure via temp queue
                    TempQueue.FetchAttributes()
                    Dim responseCount = TempQueue.ApproximateMessageCount
                    If responseCount.HasValue Then
                        processedCount = responseCount - preProcessedCount
                        preProcessedCount = processedCount + preProcessedCount
                        If processedCount >= 0 Then
                            processedCountQueue.Enqueue(processedCount)
                        Else
                            processedCount = 0
                            preProcessedCount = 0
                        End If
                    Else
                        processedCount = 0
                        preProcessedCount = 0
                    End If

                    ' measure vbia cache
                    processedCountC = RetrieveNumberOfRequestsProcessedFromCache() - preProcessedCountC
                    preProcessedCountC = processedCountC + preProcessedCountC
                    If processedCountC >= 0 Then
                    Else
                        processedCountC = 0
                        preProcessedCountC = 0
                    End If

                    'measure via table
                    processedCountT = RetrieveNumberOfRequestsProcessed() - preProcessedCountT
                    preProcessedCountT = processedCountT + preProcessedCountT
                    If processedCountT >= 0 Then
                        ' processedCountQueue.Enqueue(processedCountT)
                    Else
                        processedCountT = 0
                        preProcessedCountT = 0
                    End If

                    requestCount = RetrieveNumberOfRequests() - preRequestCount
                    preRequestCount = requestCount + preRequestCount
                    If requestCount >= 0 Then
                        requestCountQueue.Enqueue(requestCount)
                    Else
                        requestCount = 0
                        preRequestCount = 0
                    End If

                    elapsedTimeInFeedbackLoop = loopwatch.ElapsedMilliseconds
                    loopwatch.Reset()
                    loopwatch.Start()
                    If elapsedTimeInFeedbackLoop > 0 Then
                        elapsedTimeQueue.Enqueue(elapsedTimeInFeedbackLoop)
                    End If

                    Dim currentNumberOfVMs As Int32 = GetReadyInstanceCount()

                    '--------------------------------------------------------------------------------------------------------------------
                    ' Put the logic for measuring appropriate metrics as the input of the auto-scaler here
                    ' this may comprise of prediction, average, percentile, etc on the observed 
                    ' counted as stored in the queue that store the observations at runtime
                    Dim averageTimeElapsedPerFeedbackLoop = elapsedTimeQueue.GetAverageCount
                    Dim averageItemCount = itemCountQueue.GetAverageCount
                    Dim smoothedItemCont = itemCountQueue.GetSingleSmoothedCount(0.9)
                    Dim doublesmoothedItemCount = itemCountQueue.GetDoubleSmoothedCount(0.2, 0.9)
                    Dim percentile = itemCountQueue.GetPercentile(90)

                    ' we calculate request/sec
                    Dim averageRequestCount = requestCountQueue.GetAverageCount
                    If averageTimeElapsedPerFeedbackLoop > 0 Then
                        averageRequestPerSec = averageRequestCount / (averageTimeElapsedPerFeedbackLoop / 1000)
                    Else
                        averageRequestPerSec = 0
                    End If
                    ' we use little's law to calculate responhse time: http://en.wikipedia.org/wiki/Little%27s_law
                    Dim averageThroughput = processedCountQueue.GetAverageCount
                    Dim averageThroughputPerMilliseconds As Double
                    If averageTimeElapsedPerFeedbackLoop > 0 Then
                        averageThroughputPerMilliseconds = averageThroughput / averageTimeElapsedPerFeedbackLoop
                    Else
                        averageThroughputPerMilliseconds = averageThroughput
                    End If
                    Dim averageResponseTime As Double
                    If averageThroughputPerMilliseconds > 0 Then
                        averageResponseTime = averageItemCount / averageThroughputPerMilliseconds
                    End If

                    If averageResponseTime >= 0 Then
                        responseTimeQueue.Enqueue(averageResponseTime)
                    End If

                    '--------------------------------------------------------------------------------------------------------------------
                    ' normalizing the auto-scaling controller inputs

                    Dim averageRequestPerSecN = ScaleData(requestCount, 0, 100, Math.Round((maxWorkload + requestCountQueue.GetMax) / 2))
                    Dim averageResponseTimeN = ScaleData(averageResponseTime, 0, 100, Math.Round((SLOrt + responseTimeQueue.GetMax) / 2))

                    Dim monitoringDelay = watch.ElapsedMilliseconds
                    watch.Reset()


                    '--------------------------------------------------------------------------------------------------------------------
                    ' Plug your auto-scaling logic here

                    watch.Start()
                    ' here is one simple auto-scaling harcoded rule!

                    'If averageItemCount > 50 Then
                    '    ScalingDecision = 1
                    'End If

                    'If averageItemCount = 0 Then
                    '    ScalingDecision = -1
                    'End If

                    ' here is a controller (RobusT2Scale) that we plug here
                    ' convert the first and second input to Object, Matlab convention

                    'Dim controlOutputObj As Object = controller.RobusT2Scalesg(CType(averageRequestPerSecN, Object), CType(averageResponseTimeN, Object))
                    'Dim controlOutput As Double(,) = CType(controlOutputObj, Double(,))
                    ' since scaling action means adding or removing role instances then should be integer
                    'ScalingDecision = Math.Round(controlOutput(0, 0))


                    ' here we plug adaptive controller, i.e., RobusT2Scale+FQL

                    ' 1. initialize Q table
                    If Q Is Nothing Then
                        QMW = myFQL.initq()
                        Q = CType(CType(QMW, MWNumericArray).ToArray(MWArrayComponent.Real), Double(,))
                    End If

                    ' 2. calculate the action inferece by the FLC
                    Dim systemState As Double() = {averageRequestPerSecN, averageResponseTimeN, processedCount, currentNumberOfVMs}
                    Dim systemStateMW As MWNumericArray = systemState 'conversion to MWNumericArray
                    result = myFQL.fuzzy_action_calculator(2, QMW, systemStateMW, CType(epsilon, MWNumericArray))

                    Dim controlOutput As Double(,) = CType(result(0), MWNumericArray).ToArray()
                    Dim ais As Double(,) = CType(result(1), MWNumericArray).ToArray()
                    ' since scaling action means adding or removing role instances then should be integer
                    ScalingDecision = Math.Round(controlOutput(0, 0))

                    ' 3. learn, i.e., update q-values
                    If (currentState IsNot Nothing) Then
                        ' if a change is enacted to the enviornment and if the change is the action that has been issued previously, then the reward has been received
                        If systemState(3) <> currentState(3) And currentAction = systemState(3) - currentState(3) Then
                            ' update Q table
                            result = myFQL.fqlearn(2, QMW, CType(currentState, MWNumericArray), CType(systemState, MWNumericArray), CType(currentais, MWNumericArray))
                            ' type conversions
                            QMW = result(0)
                            Q = CType(CType(QMW, MWNumericArray).ToArray(MWArrayComponent.Real), Double(,))
                            LEARNMW = result(1)
                            Dim currentLEARN As Double(,) = CType(CType(LEARNMW, MWNumericArray).ToArray(), Double(,))
                            ' add current stats to the accumulated one or initialize for the first time
                            If LEARN IsNot Nothing Then
                                For i = 0 To LEARN.GetUpperBound(0)
                                    For j = 0 To LEARN.GetUpperBound(1)
                                        LEARN(i, j) = LEARN(i, j) + currentLEARN(i, j)
                                    Next
                                Next
                            Else
                                LEARN = currentLEARN
                            End If
                            ' update learning cycle number
                            epoch += 1
                            canUpdate = True
                            ' resetting snapshot
                            currentState = Nothing
                            currentAction = Nothing
                            currentais = Nothing
                        End If
                    End If

                    ' 4. exploration/exploitation strategy enforcer: after enough epoches of learning, change exploration rate and update knowledge base of the controller, replace the current .fis with the learned .fis file
                    If (epoch Mod 10) = 0 And canUpdate Then
                        myFQL.update_knowledge_base(QMW)
                        ' in each learning epoch decrease epsilon until it reaches a predetermined balance between exploration and exploitation, here 0.2
                        If epsilon >= 0.3 Then
                            epsilon -= 0.1
                        End If
                        canUpdate = False
                    End If





                    Dim planningDelay = watch.ElapsedMilliseconds
                    watch.Reset()

                    '--------------------------------------------------------------------------------------------------------------------
                    ' use the actuator functions here
                    ' evaluate the constraints and if nothing is violated enact the change, in case of number of vms we can compromise by requesting less vms

                    watch.Start()

                    Dim issued As Boolean = False
                    successfullyIssued = False
                    ' evaluating scaling constraints
                    If ScalingDecision > 0 And EnforcePolicy(ScalingDecision) Then
                        issued = True
                        ChangeNumberOfNodes(ScalingDecision)
                    ElseIf ScalingDecision > 1 Then ' consider degradated scaling out decision   
                        While ScalingDecision > 1 And issued = False
                            ScalingDecision -= 1
                            If EnforcePolicy(ScalingDecision) Then
                                issued = True
                                ChangeNumberOfNodes(ScalingDecision)
                            End If
                        End While
                    ElseIf ScalingDecision < -1 Then ' consider degradated scaling in decisions 
                        While ScalingDecision < -1 And issued = False
                            ScalingDecision += 1
                            If EnforcePolicy(ScalingDecision) Then
                                issued = True
                                ChangeNumberOfNodes(ScalingDecision)
                            End If
                        End While
                    End If

                    Dim actuationDelay = watch.ElapsedMilliseconds
                    watch.Reset()

                    '--------------------------------------------------------------------------------------------------------------------
                    ' snapsot capturing if an scaling decision has been issued
                    If successfullyIssued = True Then
                        currentState = systemState
                        currentAction = ScalingDecision
                        currentais = ais
                    End If


                    '--------------------------------------------------------------------------------------------------------------------
                    ' dump data produced for experimental analysis 

                    Dim resultEntity As New FibonacciLibrary.ResultEntity
                    resultEntity.RequestCount = requestCount
                    resultEntity.AverageRequestCount = averageRequestCount
                    resultEntity.AverageRequestPerSec = averageRequestPerSec
                    resultEntity.AverageRequestCountN = averageRequestPerSecN
                    resultEntity.ProcessedCount = processedCount
                    resultEntity.ProcessedCountCache = processedCountC
                    resultEntity.ProcessedCountTable = processedCountT
                    resultEntity.AverageThroughput = averageThroughput
                    resultEntity.AverageThroughputPerMilliseconds = averageThroughputPerMilliseconds
                    resultEntity.AverageReponseTime = averageResponseTime
                    resultEntity.AverageResponseTimeN = averageResponseTimeN
                    resultEntity.QueueSize = messageCount
                    resultEntity.AveItemCount = averageItemCount
                    resultEntity.SingleSmoothed = smoothedItemCont
                    resultEntity.DoubleSmoothed = doublesmoothedItemCount
                    resultEntity.Percentile = percentile
                    resultEntity.ControlOutput = controlOutput(0, 0)
                    resultEntity.ScalingDecision = ScalingDecision
                    resultEntity.ActionIssued = issued
                    resultEntity.SuccessfullyIssued = successfullyIssued
                    resultEntity.Enacted = RetrieveNumberOfScalingActionsEnacted()
                    resultEntity.MonitoringDelay = monitoringDelay
                    resultEntity.PlanningDelay = planningDelay
                    resultEntity.ActuationDelay = actuationDelay
                    resultEntity.ElapsedTimeInFeedbackLoop = elapsedTimeInFeedbackLoop
                    resultEntity.AveElapsedTimeInFeedbackLoop = averageTimeElapsedPerFeedbackLoop
                    ' learning data
                    resultEntity.CurrentState = array2string.Array2String(currentState)
                    resultEntity.Qtable = array2string.Matrix2String(Q)
                    resultEntity.LEARNmatrix = array2string.Matrix2String(LEARN)
                    resultEntity.Epsilon = epsilon
                    resultEntity.Epoch = epoch


                    Dim insertOperation As TableOperation = TableOperation.Insert(resultEntity)
                    ResultTable.Execute(insertOperation)

                    '--------------------------------------------------------------------------------------------------------------------
                    ' logic for delaying in the feedback control loop here

                    Thread.Sleep(Latency)

                Else
                    ' mode change latency
                    Thread.Sleep(1000)
                End If
                Trace.TraceInformation("Working", "Information")

            Catch ex As Exception
                Trace.TraceInformation("Something went wrong in Monitoring worker role")
                Trace.TraceError(ex.Message)
            End Try

        End While

    End Sub

#End Region

#Region "Worker role resiliency code here"

    Private Sub RoleEnvironmentChanging(ByVal sender As Object, ByVal e As RoleEnvironmentChangingEventArgs)
        ' If a configuration setting is changing
        If e.Changes.Any(Function(change) TypeOf change Is RoleEnvironmentConfigurationSettingChange) Then
            ' Set e.Cancel to true to restart this role instance
            e.Cancel = True
        End If
    End Sub

    Public Overrides Function OnStart() As Boolean

        ' Set the maximum number of concurrent connections 
        ' ServicePointManager.DefaultConnectionLimit = 12

        ' Restart the role upon all configuration changes
        AddHandler RoleEnvironment.Changing, AddressOf RoleEnvironmentChanging

        ' configuration variables and structures that we require for decision making are initialized here
        scaleWhileTransitioning = CType(RoleEnvironment.GetConfigurationSettingValue("ScaleWhileTransitioning"), Boolean)

        ' convert latency in seconds to miliseconds
        Latency = CType(RoleEnvironment.GetConfigurationSettingValue("PollingInterval"), Integer) * 1000

        itemCountQueue = New Analytics(WindowLength)
        processedCountQueue = New Analytics(WindowLength)
        requestCountQueue = New Analytics(WindowLength)
        elapsedTimeQueue = New Analytics(WindowLength)
        responseTimeQueue = New Analytics(WindowLength)

        minNode = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("MinimumNodes"))
        maxNode = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("MaximumNodes"))

        SERVICE_NAME = RoleEnvironment.GetConfigurationSettingValue("ServiceName")
        SUBSCRIPTION_ID = RoleEnvironment.GetConfigurationSettingValue("SubscriptionID")

        maxWorkload = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("MaximumWorkload"))
        SLOrt = Convert.ToInt32(RoleEnvironment.GetConfigurationSettingValue("SLOrt"))


        ' For information on handling configuration changes
        ' see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.

        Return MyBase.OnStart()

    End Function

    Public Overrides Sub OnStop()
        ' put necessary commands before fully exiting the feedback loop

        MyBase.OnStop()
    End Sub

#End Region

End Class
