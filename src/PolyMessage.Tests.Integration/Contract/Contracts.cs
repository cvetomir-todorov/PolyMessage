using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PolyMessage.Tests.Integration.Contract
{
    [PolyMessage, Serializable, DataContract]
    public sealed class ValidRequest {}

    [PolyMessage, Serializable, DataContract]
    public sealed class ValidResponse {}

    [PolyMessage, Serializable, DataContract]
    public sealed class AnotherValidRequest { }

    [PolyMessage, Serializable, DataContract]
    public sealed class AnotherValidResponse { }

    [Serializable, DataContract]
    public sealed class InvalidRequest {}

    [Serializable, DataContract]
    public sealed class InvalidResponse {}

    [PolyMessage(ID = 1), Serializable, DataContract]
    public sealed class RequestWithSameID {}

    [PolyMessage(ID = 1), Serializable, DataContract]
    public sealed class ResponseWithSameID {}

    public interface IContractWithoutAttributeAndInterface
    {
        [PolyRequestResponse] Task<ValidResponse> Operation(ValidRequest request);
    }

    public interface IContractWithoutOperations : IPolyContract
    {}

    [PolyContract]
    public interface IOperationWithoutAttribute : IPolyContract
    {
        Task<ValidResponse> Operation(ValidRequest request);
    }

    [PolyContract]
    public interface IOperationNotReturningTaskOfResponse
    {
        [PolyRequestResponse]
        ValidResponse Operation(ValidRequest request);
    }

    [PolyContract]
    public interface IOperationReturningResponseWithoutAttribute
    {
        [PolyRequestResponse]
        Task<InvalidResponse> Operation(ValidRequest request);
    }

    [PolyContract]
    public interface IOperationAcceptingMoreThanOneRequest
    {
        [PolyRequestResponse]
        Task<ValidResponse> Operation(ValidRequest request1, AnotherValidRequest request2);
    }

    [PolyContract]
    public interface IOperationAcceptingRequestWithoutAttribute
    {
        [PolyRequestResponse]
        Task<ValidResponse> Operation(InvalidRequest request);
    }

    [PolyContract]
    public interface IMessagesWithSameTypeID
    {
        [PolyRequestResponse]
        Task<ResponseWithSameID> Operation(RequestWithSameID request);
    }

    [PolyContract]
    public interface IOperationsWithSameRequests
    {
        [PolyRequestResponse]
        Task<ValidResponse> Operation1(ValidRequest request);

        [PolyRequestResponse]
        Task<AnotherValidResponse> Operation2(ValidRequest request);
    }

    [PolyContract]
    public interface IOperationsWithSameResponses
    {
        [PolyRequestResponse]
        Task<ValidResponse> Operation1(ValidRequest request);

        [PolyRequestResponse]
        Task<ValidResponse> Operation2(AnotherValidRequest request);
    }

    [PolyContract]
    public interface IOperationsWithSameMessagesAsRequestAndResponse
    {
        [PolyRequestResponse]
        Task<ValidResponse> Operation1(ValidRequest request);

        [PolyRequestResponse]
        Task<AnotherValidResponse> Operation2(ValidResponse request);
    }

    public interface IMultipleErrors
    {
        Task<ValidResponse> Operation1(ValidRequest request);

        [PolyRequestResponse]
        InvalidResponse Operation2(InvalidRequest request);
    }
}
