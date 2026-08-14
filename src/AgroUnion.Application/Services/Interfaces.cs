using AgroUnion.Application.Contracts;
using AgroUnion.Domain.Entities;

namespace AgroUnion.Application.Services;

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken cancellationToken = default);
}

public interface IAgroUnionService
{
    Task<Guid> SubmitInterestAsync(InterestApplicationRequest request, CancellationToken ct = default);
    Task<Guid> SubmitContactAsync(ContactRequest request, CancellationToken ct = default);
    Task<AdminDashboardDto> GetAdminDashboardAsync(string? producerUserId = null, CancellationToken ct = default);
    Task<ProducerDashboardDto> GetProducerDashboardAsync(string userId, CancellationToken ct = default);
    Task<BuyerDashboardDto> GetBuyerDashboardAsync(string userId, bool isCompany, CancellationToken ct = default);
    Task<ApprovalResult> ApproveApplicationAsync(Guid id, string adminUserId, CancellationToken ct = default);
    Task UpdateApplicationAsync(Guid id, ApplicationStatus status, string? notes, string adminUserId, CancellationToken ct = default);
    Task<Guid> SaveProductionAsync(string producerUserId, Guid? id, ProductionRequest request, CancellationToken ct = default);
    Task DeleteProductionAsync(string producerUserId, Guid id, CancellationToken ct = default);
    Task SubmitCounterOfferAsync(string buyerUserId, Guid offerId, CounterOfferRequest request, CancellationToken ct = default);
    Task JoinSupplyOrderAsync(string producerUserId, Guid orderId, SupplyParticipationRequest request, CancellationToken ct = default);
    Task<Guid> CreatePriceItemAsync(string publisherUserId, PriceListRequest request, CancellationToken ct = default);
    Task<Guid> CreateDealAsync(string adminUserId, DealRequest request, CancellationToken ct = default);
    Task ConfirmDealSideAsync(string userId, Guid dealId, bool buySide, bool isAdmin, CancellationToken ct = default);
    Task<Guid> CreateSupplyOrderAsync(SupplyOrderRequest request, CancellationToken ct = default);
    Task CloseSupplyOrderAsync(Guid orderId, CancellationToken ct = default);
    Task ActivateContractAsync(Guid contractId, CancellationToken ct = default);
    Task<string> ResetPasswordAsync(string userId, CancellationToken ct = default);
    Task ChangeUserRoleAsync(string userId, string role, CancellationToken ct = default);
    Task DeletePriceItemAsync(string requesterUserId, bool isAdmin, Guid id, CancellationToken ct = default);
    Task MarkContactReadAsync(Guid id, CancellationToken ct = default);
    Task SchedulePickupAsync(string buyerUserId, Guid dealId, PickupRequest request, CancellationToken ct = default);
    Task SetUserActiveAsync(string userId, bool active, CancellationToken ct = default);
    Task DeletePersonalDataAsync(string userId, CancellationToken ct = default);
    Task<string> ExportTransactionsCsvAsync(string userId, bool isAdmin, CancellationToken ct = default);
    Task<string> ExportMarginCsvAsync(CancellationToken ct = default);
    Task SaveProducerProfileAsync(string producerUserId, ProducerProfileRequest request, string adminUserId, CancellationToken ct = default);
    Task<Guid> AddPartnerDocumentAsync(string producerUserId, PartnerDocumentRequest request, string adminUserId, CancellationToken ct = default);
    Task DeletePartnerDocumentAsync(Guid id, string adminUserId, CancellationToken ct = default);
    Task<Guid> AddPartnerInvoiceAsync(string producerUserId, PartnerInvoiceRequest request, string adminUserId, CancellationToken ct = default);
    Task DeletePartnerInvoiceAsync(Guid id, string adminUserId, CancellationToken ct = default);
    Task<Guid> AddPartnerFinancialEntryAsync(string producerUserId, PartnerFinancialEntryRequest request, string adminUserId, CancellationToken ct = default);
    Task DeletePartnerFinancialEntryAsync(Guid id, string adminUserId, CancellationToken ct = default);
    Task<Guid> SaveProducerDeliveryAsync(string producerUserId, Guid? id, ProducerDeliveryRequest request, string adminUserId, CancellationToken ct = default);
    Task DeleteProducerDeliveryAsync(Guid id, string adminUserId, CancellationToken ct = default);
}

public interface IJwtTokenService
{
    JwtTokenResult Create(string userId, string email, string role);
}
