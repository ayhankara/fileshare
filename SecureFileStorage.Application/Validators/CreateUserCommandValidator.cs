using FluentValidation;
using SecureFileStorage.Application.Commands;

namespace SecureFileStorage.Application.Validators
{

    public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email alanı boş olamaz.")
                .EmailAddress().WithMessage("Geçerli bir email adresi giriniz.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Şifre boş olamaz.")
                .MinimumLength(6).WithMessage("Şifre en az 6 karakter olmalıdır.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Ad alanı boş olamaz.")
                .MaximumLength(50).WithMessage("Ad en fazla 50 karakter olabilir.");

            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Soyad alanı boş olamaz.")
                .MaximumLength(50).WithMessage("Soyad en fazla 50 karakter olabilir.");
        }
    }

}
