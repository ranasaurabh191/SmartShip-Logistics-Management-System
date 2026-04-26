using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SmartShip.PaymentService.Core.Interfaces.Persistence;
using SmartShip.PaymentService.Core.Interfaces.Repositories;
using SmartShip.PaymentService.Core.Interfaces.Services;
using SmartShip.PaymentService.Tests.Mocks;
using PaymentSvc = SmartShip.PaymentService.Core.Services.PaymentService;

namespace SmartShip.PaymentService.Tests.Helpers;

public abstract class PaymentServiceTestBase
{
    protected readonly Mock<IPaymentRepository> _paymentRepo = new();
    protected readonly Mock<ISagaCorrelationRepository> _sagaRepo = new();
    protected readonly Mock<IUnitOfWork> _uow = new();
    protected readonly MockPublishEndpoint _publisher = new();
    protected const int UserId = 29;

    protected static IRazorpayClient MockRazorpay() => new MockRazorpayClient();
    protected PaymentSvc BuildService(
    IHttpClientFactory? httpClientFactory = null,
    int userId = UserId) =>
    new(
        _paymentRepo.Object,
        _sagaRepo.Object,
        _uow.Object,
        _publisher,
        NullLogger<PaymentSvc>.Instance,
        httpClientFactory ?? MockHttpClientFactory.WithNotFound(),
        MockHttpContext.WithUserId(userId),
        MockRazorpay()                         
    );

    protected PaymentSvc BuildUnauthenticatedService(
    IHttpClientFactory? httpClientFactory = null) =>
    new(
        _paymentRepo.Object,
        _sagaRepo.Object,
        _uow.Object,
        _publisher,
        NullLogger<PaymentSvc>.Instance,
        httpClientFactory ?? MockHttpClientFactory.WithNotFound(),
        MockHttpContext.Unauthenticated(),
        MockRazorpay()                          
    );
}