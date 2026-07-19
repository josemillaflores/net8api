# MicroservicesSolution with Azure Developer CLI

This solution is configured to be deployed to Azure using the [Azure Developer CLI (`azd`)](https://github.com/Azure/azure-dev).

## Prerequisites

- [Azure Developer CLI (`azd`)](https://aka.ms/azd-install)
- [Docker](https://www.docker.com/products/docker-desktop)
- An Azure account with an active subscription.

## Deployment

1. **Login to Azure:**
   ```bash
   azd auth login
   ```

2. **Provision and Deploy:**
   Run the following command to provision the Azure resources and deploy the application:
   ```bash
   azd up
   ```

   This command will:
   - Create a new resource group in Azure.
   - Provision the necessary resources, including Azure Container Apps for each microservice and the Blazor frontend.
   - Build the container images for each service.
   - Push the container images to Azure Container Registry.
   - Deploy the services to Azure Container Apps.

3. **Access the application:**
   Once the deployment is complete, `azd` will output the URL for the Blazor web application.

## Clean up

To delete all the resources created by `azd`, run the following command:

```bash
azd down
```
