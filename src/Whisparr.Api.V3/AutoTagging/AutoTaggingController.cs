using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.AutoTagging;
using NzbDrone.Core.AutoTagging.Specifications;
using Whisparr.Http;
using Whisparr.Http.REST;
using Whisparr.Http.REST.Attributes;

namespace Whisparr.Api.V3.AutoTagging
{
    [V3ApiController]
    public class AutoTaggingController : RestController<AutoTaggingResource>
    {
        private readonly IAutoTaggingService _autoTaggingService;
        private readonly List<IAutoTaggingSpecification> _specifications;

        public AutoTaggingController(IAutoTaggingService autoTaggingService,
                                  List<IAutoTaggingSpecification> specifications)
        {
            _autoTaggingService = autoTaggingService;
            _specifications = specifications;

            SharedValidator.RuleFor(c => c.Name).NotEmpty();
            SharedValidator.RuleFor(c => c.Name)
                .Must((v, c) => !_autoTaggingService.All().Any(f => f.Name == c && f.Id != v.Id)).WithMessage("Must be unique.");
            SharedValidator.RuleFor(c => c.Tags).NotEmpty();
            SharedValidator.RuleFor(c => c.Specifications).NotEmpty();
            SharedValidator.RuleFor(c => c).Custom((autoTag, context) =>
            {
                if (!autoTag.Specifications.Any())
                {
                    context.AddFailure("Must contain at least one Condition");
                }

                if (autoTag.Specifications.Any(s => s.Name.IsNullOrWhiteSpace()))
                {
                    context.AddFailure("Condition name(s) cannot be empty or consist of only spaces");
                }
            });
        }

        protected override AutoTaggingResource GetResourceById(int id)
        {
            return _autoTaggingService.GetById(id).ToResource();
        }

        [RestPostById]
        [Consumes("application/json")]
        public ActionResult<AutoTaggingResource> Create(AutoTaggingResource autoTagResource)
        {
            var model = autoTagResource.ToModel(_specifications);
            return Created(_autoTaggingService.Insert(model).Id);
        }

        [RestPutById]
        [Consumes("application/json")]
        public ActionResult<AutoTaggingResource> Update(AutoTaggingResource resource)
        {
            var model = resource.ToModel(_specifications);
            _autoTaggingService.Update(model);

            return Accepted(model.Id);
        }

        [HttpGet]
        [Produces("application/json")]
        public List<AutoTaggingResource> GetAll()
        {
            return _autoTaggingService.All().ToResource();
        }

        [RestDeleteById]
        public void DeleteFormat(int id)
        {
            _autoTaggingService.Delete(id);
        }

        [HttpGet("schema")]
        public object GetTemplates()
        {
            var schema = _specifications.OrderBy(x => x.Order).Select(x => x.ToSchema()).ToList();

            return schema;
        }
    }
}
