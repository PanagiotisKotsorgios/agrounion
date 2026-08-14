using AgroUnion.Application.Contracts;
using FluentValidation;

namespace AgroUnion.Application.Services;

public sealed class InterestApplicationValidator : AbstractValidator<InterestApplicationRequest>
{
    public InterestApplicationValidator()
    {
        RuleFor(x => x.FullNameOrCompany).NotEmpty().WithMessage("Συμπληρώστε ονοματεπώνυμο ή επωνυμία.").MaximumLength(180);
        RuleFor(x => x.Region).NotEmpty().WithMessage("Συμπληρώστε περιοχή.").MaximumLength(120);
        RuleFor(x => x.ProductInterest).NotEmpty().WithMessage("Επιλέξτε προϊόν ή υπηρεσία.");
        RuleFor(x => x.Phone).NotEmpty().WithMessage("Συμπληρώστε τηλέφωνο.").Matches(@"^[0-9+ ()-]{8,20}$").WithMessage("Το τηλέφωνο δεν είναι έγκυρο.");
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Το email δεν είναι έγκυρο.");
        RuleFor(x => x.Message).MaximumLength(3000);
        RuleFor(x => x.Consent).Equal(true).WithMessage("Απαιτείται αποδοχή της πολιτικής απορρήτου.");
        RuleFor(x => x.Website).Empty().WithMessage("Η υποβολή απορρίφθηκε.");
    }
}

public sealed class ContactRequestValidator : AbstractValidator<ContactRequest>
{
    public ContactRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().WithMessage("Συμπληρώστε το όνομά σας.").MaximumLength(180);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Το email δεν είναι έγκυρο.");
        RuleFor(x => x.Message).NotEmpty().WithMessage("Γράψτε το μήνυμά σας.").MaximumLength(3000);
        RuleFor(x => x.Website).Empty().WithMessage("Η υποβολή απορρίφθηκε.");
    }
}

public sealed class ProductionRequestValidator : AbstractValidator<ProductionRequest>
{
    public ProductionRequestValidator()
    {
        RuleFor(x => x.Product).NotEmpty().WithMessage("Συμπληρώστε προϊόν.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Η ποσότητα πρέπει να είναι μεγαλύτερη από μηδέν.");
        RuleFor(x => x.Region).NotEmpty().WithMessage("Συμπληρώστε περιοχή.");
        RuleFor(x => x.AvailableTo).GreaterThanOrEqualTo(x => x.AvailableFrom).WithMessage("Η λήξη διαθεσιμότητας δεν μπορεί να προηγείται της έναρξης.");
    }
}

public sealed class CounterOfferValidator : AbstractValidator<CounterOfferRequest>
{
    public CounterOfferValidator()
    {
        RuleFor(x => x.PricePerUnit).GreaterThan(0).WithMessage("Η τιμή πρέπει να είναι θετική.");
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Η ποσότητα πρέπει να είναι θετική.");
    }
}
