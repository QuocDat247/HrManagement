# Customer Build Profiles

Customer Profile định nghĩa cấu hình build-time cho từng bản triển khai
của HR Management, bao gồm edition, mã khách hàng, release channel,
branding và các module tính năng được bật hoặc tắt.

Customer Profile không phải là cơ chế phân quyền hoặc licensing.
Authorization của ứng dụng vẫn là ranh giới bảo mật cho quyền người dùng.

## Vị trí

Các profile đang hoạt động:

`build/customer-profiles/`

Template tạo profile mới:

`build/customer-profile-template/`

Không đặt template vào thư mục `customer-profiles`, vì validator sẽ
quét toàn bộ file `.props` trong thư mục đó.

## Tạo customer profile mới

Ví dụ:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File build/new-customer-profile.ps1 `
  -Profile Customer-A `
  -CustomerCode CUSTOMER-A `
  -CustomerDisplayName "Customer A" `
  -Edition Standard `
  -SupportContact "support@example.invalid"
```

Các module được bật mặc định.

Có thể tắt từng module bằng:

`-DisableEmployees`

`-DisableOrganization`

`-DisableTimeManagement`

`-DisablePayroll`

## Edition hợp lệ

`Essential`

`Standard`

`Professional`

## Release channel hợp lệ

`Development`

`CustomerPreview`

`ReleaseCandidate`

`Production`

Profile mới mặc định dùng `CustomerPreview`.

## Validate customer profiles

Chạy:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File build/validate-customer-profiles.ps1
```

Validator kiểm tra property bắt buộc, feature flag, CustomerCode trùng,
edition, release channel, branding và quy tắc đặt tên profile.

Installer pipeline cũng tự động chạy validator.

## Build thử một profile

```powershell
dotnet build `
  HrManagement.Desktop/HrManagement.Desktop.csproj `
  -p:HrCustomerProfile=Customer-A
```

Có thể chạy ứng dụng ở Demo mode để kiểm tra branding,
build identity và các module điều hướng.

## Build installer

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File installer/build-installer.ps1 `
  -Profile Customer-A
```

Mỗi release tạo ra installer, file SHA-256 và
`release-manifest.json`.

## Verify customer release

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File build/verify-customer-release.ps1 `
  -Profile Customer-A
```

Đối với release chính thức được build từ source tree sạch:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
  -File build/verify-customer-release.ps1 `
  -Profile Customer-A `
  -RequireCleanSource
```

## Quy trình customer release

Trước khi giao bản build cho khách hàng:

1. Validate toàn bộ customer profile.
2. Build và test profile được chọn.
3. Review branding, edition và feature set.
4. Commit và push source.
5. Xác nhận `git status` sạch.
6. Build installer.
7. Verify customer release với `-RequireCleanSource`.
8. Review `release-manifest.json`.
9. Giao installer, checksum và manifest cùng nhau.

## Lưu ý bảo mật

Không lưu password, recovery code, API key, private key,
database credential hoặc bí mật của khách hàng trong profile.

Feature flag kiểm soát module có mặt trong customer build.
Nó không thay thế Authorization.