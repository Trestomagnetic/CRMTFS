using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace CrmPlayground
{
    public class OrganizationServiceDecorator : IOrganizationService
    {
        private IOrganizationService _organizationService;

        public OrganizationServiceDecorator(IOrganizationService organizationService)
        {
            _organizationService = organizationService;
        }

        public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            _organizationService.Associate(entityName, entityId, relationship, relatedEntities);
        }

        public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
        {
            _organizationService.Disassociate(entityName, entityId, relationship, relatedEntities);
        }

        public Guid Create(Entity entity)
        {
            return _organizationService.Create(entity);
        }

        public void Update(Entity entity)
        {
            _organizationService.Update(entity);
        }

        public void Delete(string entityName, Guid id)
        {
            _organizationService.Delete(entityName, id);
        }

        public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
        {
            return _organizationService.Retrieve(entityName, id, columnSet);
        }

        public EntityCollection RetrieveMultiple(QueryBase query)
        {
            return _organizationService.RetrieveMultiple(query);
        }

        public OrganizationResponse Execute(OrganizationRequest request)
        {
            if (OnBeforeExecute != null)
                OnBeforeExecute(request);

            OrganizationResponse response;
            try {
                response = _organizationService.Execute(request);

                if (OnAfterExecute != null)
                    OnAfterExecute(response, null);

            } catch (Exception ex) {
                if (OnAfterExecute != null)
                    OnAfterExecute(null, ex);
                throw;
            }

            return response;
        }

        public event Action<OrganizationRequest> OnBeforeExecute;

        public event Action<OrganizationResponse, Exception> OnAfterExecute;
    }
}
