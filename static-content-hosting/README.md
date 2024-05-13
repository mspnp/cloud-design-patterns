# Static Content Hosting Pattern

This document describes the Static Content Hosting Pattern example from the guide [Cloud Design Patterns](http://aka.ms/Cloud-Design-Patterns).

## Before you start

Ensure that you have installed all of the software prerequisites.

The example demonstrates operational aspects of static websites hosted in Azure Storage Accounts with support for static websites. Therefore, you will need to deploy this example in Azure.

## About the Example

This example shows how to reference static content from a publicly accessible storage service. The example contains steps to host documents like HTML, JavaScript files and images into a  Azure storage account. This type of content is typically deployed to the storage account as part of the application deployment process. However, to simplify the example as well as concentrate on the pattern itself, files are uploaded to the storage account by following the steps below.

   - The JavaScript, image and stylesheet content are all referenced in the file src/Index.html.

When navigating the website from your browser, all static resources are served out of the storage account, as opposed to being delivered by the application server.

## Deploy the Example

> Note: at the time of writing this, [Azure storage account emulator(Azurite)](https://github.com/Azure/Azurite) doesn't provide with support to the static website feature.

1. prereq
\<TODO\>

1. Control Plane: deploy Azure storage account
\<TODO\>

1. Data Plane: Enable static website
\<TODO\>

1. upload files
\<TODO\>

1. validate
\<TODO\>
