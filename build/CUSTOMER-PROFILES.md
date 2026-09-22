\# Customer Build Profiles



Customer profiles define build-time identity, branding, edition,

release channel, and enabled feature modules for HR Management.



They are configuration for customer builds. They are not an

authorization or licensing boundary.



\## Profile location



Active profiles are stored in:



`build/customer-profiles/`



The reusable template is stored separately in:



`build/customer-profile-template/`



Do not place the template inside the active profile directory.



\## Create a new customer profile



Use:



```powershell

powershell -NoProfile -ExecutionPolicy Bypass `

&#x20; -File build/new-customer-profile.ps1 `

&#x20; -Profile Customer-A `

&#x20; -CustomerCode CUSTOMER-A `

&#x20; -CustomerDisplayName "Customer A" `

&#x20; -Edition Standard `

&#x20; -SupportContact "support@example.invalid"