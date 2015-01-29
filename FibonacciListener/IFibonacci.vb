Imports System.ServiceModel

' NOTE: You can use the "Rename" command on the context menu to change the interface name "IFibonacci" in both code and config file together.
<ServiceContract()>
Public Interface IFibonacci

    <OperationContract()> _
    Function Ping() As Boolean
    <OperationContract()> _
    Function Calculate(ByVal n As Integer) As Int64
    <OperationContract()> _
    Sub BeginCalculate(ByVal n As Integer)


End Interface
