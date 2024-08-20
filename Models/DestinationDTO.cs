using FluentValidation;
using System.Collections.Generic;

namespace api_my_web.Models
{
    public class DestinationDTO
    {
        public string Name { get; set; } = string.Empty;
        public string DescriptionEnglish { get; set; } = string.Empty;
        public List<string> AttractionsEnglish { get; set; } = new List<string>();
        public List<string> LocalDishesEnglish { get; set; } = new List<string>();
    }

    public class DestinationDTOValidator : AbstractValidator<DestinationDTO>
    {
        public DestinationDTOValidator()
        {
            RuleFor(d => d.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(20).WithMessage("Name cannot be more than 20 characters.");

            RuleFor(d => d.DescriptionEnglish)
                .NotEmpty().WithMessage("Description is required.");

            RuleFor(d => d.AttractionsEnglish)
                .NotNull().WithMessage("Attractions are required.")
                .NotEmpty().WithMessage("Attractions cannot be empty.");

            RuleFor(d => d.LocalDishesEnglish)
                .NotNull().WithMessage("Local dishes are required.")
                .NotEmpty().WithMessage("Local dishes cannot be empty.");
        }
    }
}
