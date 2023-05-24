provider "azurerm" {
  features {}

  client_id       = "0ee2a41a-f36a-47a2-bbdc-4cf163b4168e"
  client_secret   = "SMm8Q~fiRKW~Vo-bvQj10hP8ol4xKNnGsRb~vba~"
  tenant_id       = "097f4373-1669-4974-bf05-78c307d1b80a"
  subscription_id = "93b2be68-f1c2-4eaf-93ce-bd6ab3b51394"
}

resource "azurerm_resource_group" "rg-TextToSpeech" {
  name     = "rg-TextToSpeech"
  location = "West Europe"
}

resource "azurerm_app_service_plan" "asp_rgTextToSpeech_afad" {
  name                = "ASP-rgTextToSpeech-afad"
  location            = azurerm_resource_group.rg-TextToSpeech.location
  resource_group_name = azurerm_resource_group.rg-TextToSpeech.name

  sku {
    tier = "Standard"
    size = "S1"
  }
}

resource "azurerm_storage_account" "satexttospeech" {
  name                     = "satexttospeech"
  resource_group_name      = azurerm_resource_group.rg-TextToSpeech.name
  location                 = azurerm_resource_group.rg-TextToSpeech.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_app_service_plan" "example" {
  name                = "example-appserviceplan"
  location            = azurerm_resource_group.rg-TextToSpeech.location
  resource_group_name = azurerm_resource_group.rg-TextToSpeech.name

  sku {
    tier = "Standard"
    size = "S1"
  }
}

resource "azurerm_function_app" "FunctionTwimlTrigger" {
  name                       = "FunctionTwimlTrigger"
  location                   = azurerm_resource_group.rg-TextToSpeech.location
  resource_group_name        = azurerm_resource_group.rg-TextToSpeech.name
  app_service_plan_id        = azurerm_app_service_plan.asp_rgTextToSpeech_afad.id
  storage_account_name       = azurerm_storage_account.satexttospeech.name
  storage_account_access_key = azurerm_storage_account.satexttospeech.primary_access_key
}
