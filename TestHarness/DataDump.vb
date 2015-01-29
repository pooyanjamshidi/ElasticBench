Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Text
Imports FibonacciLibrary

Imports Microsoft.WindowsAzure.Storage
Imports Microsoft.WindowsAzure.Storage.Auth
Imports Microsoft.WindowsAzure.Storage.Table

Imports System.Configuration
Imports CsvHelper
Imports System.IO

''' <summary></summary>
''' <remarks>This platform is implemented to facilitate research in auto-scaling and self-adaptive clouds.</remarks>
''' <authors>Pooyan Jamshidi (pooyan.jamshidi@gmail.com)</authors>
Public Class DataDump
    Private Shared _resulttable As CloudTable
    Private Shared ReadOnly Property ResultTable As CloudTable
        Get
            If _resulttable Is Nothing Then InitializeResultTable()
            Return _resulttable
        End Get
    End Property
    Private Shared Sub InitializeResultTable()
        Dim account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings("DataConnectionString"))
        Dim tableClient = account.CreateCloudTableClient()
        _resulttable = tableClient.GetTableReference("result")
        _resulttable.CreateIfNotExists()
    End Sub
    Public Sub DumpExperimentData(ByVal startTime As DateTime, ByVal endTime As DateTime)
        ' retrieving data       
        ' filtering data regarding this experiment
        Dim retrieveQuery As TableQuery(Of ResultEntity) = New TableQuery(Of ResultEntity)().
            Where(TableQuery.CombineFilters(TableQuery.GenerateFilterConditionForDate("TimeInserted", QueryComparisons.GreaterThanOrEqual, startTime),
                                            TableOperators.And,
                                            TableQuery.GenerateFilterConditionForDate("TimeInserted", QueryComparisons.LessThanOrEqual, endTime)))

        Dim result As IEnumerable(Of ResultEntity) = ResultTable.ExecuteQuery(retrieveQuery)

        ' dump data
        Dim csv As CsvWriter
        Dim textWriter = New StreamWriter("data.csv")
        csv = New CsvWriter(textWriter)
        csv.WriteRecords(result)
        textWriter.Close()
    End Sub

    Public Sub DumpAllExperimentsData()
        ' retrieving all data
        Dim retrieveQuery As TableQuery(Of ResultEntity) = New TableQuery(Of ResultEntity)().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, String.Empty))
        Dim result As IEnumerable(Of ResultEntity) = ResultTable.ExecuteQuery(retrieveQuery)

        ' dump all data in result table
        Dim csv As CsvWriter
        Dim textWriter = New StreamWriter("data_all.csv")
        csv = New CsvWriter(textWriter)
        csv.WriteRecords(result)
        textWriter.Close()
    End Sub
End Class
