using HrManagement.Application.Employees.Profiles;
using HrManagement.Application.Employees.Profiles.Completion;
using HrManagement.Domain.Employees;
using HrManagement.Domain.Employees.Profiles;

namespace HrManagement.Tests.Employees;

public sealed class EmployeeProfileCompletionServiceTests
{
    [Fact]
    public async Task EvaluateAsync_WhenSourcesSucceed_ComposesDataAndReturnsPolicyResult()
    {
        Employee employee =
            CreateEmployee();

        var personalProfile =
            new EmployeePersonalProfileDetails(
                employee.Id,
                HasProfile: true,
                PreferredName: "An",
                Gender: EmployeeGender.Male,
                Nationality: "Việt Nam",
                PlaceOfBirth: "Hà Nội");

        var addresses =
            new EmployeeAddressBookDetails(
                employee.Id,
                PermanentAddress:
                    CreateAddress(
                        EmployeeAddressType.Permanent),
                CurrentAddress:
                    CreateAddress(
                        EmployeeAddressType.Current));

        IReadOnlyList<EmployeeEmergencyContactDetails>
            emergencyContacts =
            [
                new EmployeeEmergencyContactDetails(
                    Guid.NewGuid(),
                    "Nguyễn Văn Bình",
                    "Cha",
                    "0901000001",
                    null,
                    IsPrimary: true)
            ];

        IReadOnlyList<EmployeeIdentificationRecordDetails>
            identificationRecords =
            [
                new EmployeeIdentificationRecordDetails(
                    Guid.NewGuid(),
                    EmployeeIdentificationType.NationalId,
                    "012345678901",
                    IssueDate: null,
                    ExpiryDate: null,
                    IssuingAuthority: null,
                    PlaceOfIssue: null,
                    IssuingCountry: "Việt Nam")
            ];

        var personalService =
            new StubPersonalProfileService
            {
                Result =
                    personalProfile
            };

        var addressService =
            new StubAddressService
            {
                Result =
                    addresses
            };

        var emergencyContactService =
            new StubEmergencyContactService
            {
                Result =
                    emergencyContacts
            };

        var identificationService =
            new StubIdentificationRecordService
            {
                Result =
                    identificationRecords
            };

        var expectedResult =
            new EmployeeProfileCompletionResult(
                IsComplete: true,
                RequiresCompletion: false,
                MissingRequirements:
                    Array.Empty<
                        EmployeeProfileRequirement>());

        var policy =
            new RecordingCompletionPolicy
            {
                Result =
                    expectedResult
            };

        var service =
            CreateService(
                personalService,
                addressService,
                emergencyContactService,
                identificationService,
                policy);

        EmployeeProfileCompletionResult result =
            await service.EvaluateAsync(
                employee);

        Assert.Same(
            expectedResult,
            result);

        Assert.Equal(
            1,
            personalService.GetCallCount);

        Assert.Equal(
            1,
            addressService.GetCallCount);

        Assert.Equal(
            1,
            emergencyContactService.GetCallCount);

        Assert.Equal(
            1,
            identificationService.GetCallCount);

        Assert.Equal(
            employee.Id,
            personalService.LastEmployeeId);

        Assert.Equal(
            employee.Id,
            addressService.LastEmployeeId);

        Assert.Equal(
            employee.Id,
            emergencyContactService.LastEmployeeId);

        Assert.Equal(
            employee.Id,
            identificationService.LastEmployeeId);

        Assert.Equal(
            1,
            policy.EvaluateCallCount);

        Assert.NotNull(
            policy.LastData);

        Assert.Same(
            employee,
            policy.LastData!.Employee);

        Assert.Same(
            personalProfile,
            policy.LastData.PersonalProfile);

        Assert.Same(
            addresses,
            policy.LastData.Addresses);

        Assert.Same(
            emergencyContacts,
            policy.LastData.EmergencyContacts);

        Assert.Same(
            identificationRecords,
            policy.LastData.IdentificationRecords);
    }

    [Fact]
    public async Task EvaluateAsync_PassesCancellationTokenToEverySource()
    {
        Employee employee =
            CreateEmployee();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        var personalService =
            new StubPersonalProfileService();

        var addressService =
            new StubAddressService();

        var emergencyContactService =
            new StubEmergencyContactService();

        var identificationService =
            new StubIdentificationRecordService();

        var policy =
            new RecordingCompletionPolicy();

        var service =
            CreateService(
                personalService,
                addressService,
                emergencyContactService,
                identificationService,
                policy);

        await service.EvaluateAsync(
            employee,
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            personalService.LastCancellationToken);

        Assert.Equal(
            cancellationToken,
            addressService.LastCancellationToken);

        Assert.Equal(
            cancellationToken,
            emergencyContactService.LastCancellationToken);

        Assert.Equal(
            cancellationToken,
            identificationService.LastCancellationToken);
    }

    [Fact]
    public async Task EvaluateAsync_StartsAllSourcesBeforeWaitingForCompletion()
    {
        Employee employee =
            CreateEmployee();

        var personalCompletionSource =
            new TaskCompletionSource<
                EmployeePersonalProfileDetails>(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        var addressCompletionSource =
            new TaskCompletionSource<
                EmployeeAddressBookDetails>(
                    TaskCreationOptions
                        .RunContinuationsAsynchronously);

        var emergencyCompletionSource =
            new TaskCompletionSource<
                IReadOnlyList<
                    EmployeeEmergencyContactDetails>>(
                        TaskCreationOptions
                            .RunContinuationsAsynchronously);

        var identificationCompletionSource =
            new TaskCompletionSource<
                IReadOnlyList<
                    EmployeeIdentificationRecordDetails>>(
                        TaskCreationOptions
                            .RunContinuationsAsynchronously);

        var personalService =
            new StubPersonalProfileService
            {
                ResultTask =
                    personalCompletionSource.Task
            };

        var addressService =
            new StubAddressService
            {
                ResultTask =
                    addressCompletionSource.Task
            };

        var emergencyContactService =
            new StubEmergencyContactService
            {
                ResultTask =
                    emergencyCompletionSource.Task
            };

        var identificationService =
            new StubIdentificationRecordService
            {
                ResultTask =
                    identificationCompletionSource.Task
            };

        var policy =
            new RecordingCompletionPolicy();

        var service =
            CreateService(
                personalService,
                addressService,
                emergencyContactService,
                identificationService,
                policy);

        Task<EmployeeProfileCompletionResult>
            evaluationTask =
                service.EvaluateAsync(
                    employee);

        Assert.Equal(
            1,
            personalService.GetCallCount);

        Assert.Equal(
            1,
            addressService.GetCallCount);

        Assert.Equal(
            1,
            emergencyContactService.GetCallCount);

        Assert.Equal(
            1,
            identificationService.GetCallCount);

        Assert.False(
            evaluationTask.IsCompleted);

        Assert.Equal(
            0,
            policy.EvaluateCallCount);

        personalCompletionSource.SetResult(
            CreatePersonalProfile(
                employee.Id));

        addressCompletionSource.SetResult(
            CreateAddressBook(
                employee.Id));

        emergencyCompletionSource.SetResult(
            Array.Empty<
                EmployeeEmergencyContactDetails>());

        identificationCompletionSource.SetResult(
            Array.Empty<
                EmployeeIdentificationRecordDetails>());

        await evaluationTask;

        Assert.Equal(
            1,
            policy.EvaluateCallCount);
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceFails_PropagatesExceptionAndDoesNotEvaluatePolicy()
    {
        Employee employee =
            CreateEmployee();

        var personalService =
            new StubPersonalProfileService();

        var addressService =
            new StubAddressService
            {
                Exception =
                    new InvalidOperationException(
                        "Test address failure.")
            };

        var emergencyContactService =
            new StubEmergencyContactService();

        var identificationService =
            new StubIdentificationRecordService();

        var policy =
            new RecordingCompletionPolicy();

        var service =
            CreateService(
                personalService,
                addressService,
                emergencyContactService,
                identificationService,
                policy);

        InvalidOperationException exception =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () =>
                        service.EvaluateAsync(
                            employee));

        Assert.Equal(
            "Test address failure.",
            exception.Message);

        Assert.Equal(
            1,
            personalService.GetCallCount);

        Assert.Equal(
            1,
            addressService.GetCallCount);

        Assert.Equal(
            1,
            emergencyContactService.GetCallCount);

        Assert.Equal(
            1,
            identificationService.GetCallCount);

        Assert.Equal(
            0,
            policy.EvaluateCallCount);
    }

    [Fact]
    public async Task EvaluateAsync_WhenSourceIsCancelled_PropagatesCancellationAndDoesNotEvaluatePolicy()
    {
        Employee employee =
            CreateEmployee();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        CancellationToken cancellationToken =
            cancellationTokenSource.Token;

        var personalService =
            new StubPersonalProfileService
            {
                ResultTask =
                    Task.FromCanceled<
                        EmployeePersonalProfileDetails>(
                            cancellationToken)
            };

        var addressService =
            new StubAddressService();

        var emergencyContactService =
            new StubEmergencyContactService();

        var identificationService =
            new StubIdentificationRecordService();

        var policy =
            new RecordingCompletionPolicy();

        var service =
            CreateService(
                personalService,
                addressService,
                emergencyContactService,
                identificationService,
                policy);

        await Assert.ThrowsAnyAsync<
            OperationCanceledException>(
                () =>
                    service.EvaluateAsync(
                        employee,
                        cancellationToken));

        Assert.Equal(
            0,
            policy.EvaluateCallCount);
    }

    [Fact]
    public async Task EvaluateAsync_WhenEmployeeIsNull_ThrowsArgumentNullException()
    {
        var personalService =
            new StubPersonalProfileService();

        var addressService =
            new StubAddressService();

        var emergencyContactService =
            new StubEmergencyContactService();

        var identificationService =
            new StubIdentificationRecordService();

        var policy =
            new RecordingCompletionPolicy();

        var service =
            CreateService(
                personalService,
                addressService,
                emergencyContactService,
                identificationService,
                policy);

        await Assert.ThrowsAsync<
            ArgumentNullException>(
                () =>
                    service.EvaluateAsync(
                        null!));

        Assert.Equal(
            0,
            personalService.GetCallCount);

        Assert.Equal(
            0,
            addressService.GetCallCount);

        Assert.Equal(
            0,
            emergencyContactService.GetCallCount);

        Assert.Equal(
            0,
            identificationService.GetCallCount);

        Assert.Equal(
            0,
            policy.EvaluateCallCount);
    }

    private static EmployeeProfileCompletionService
        CreateService(
            IEmployeePersonalProfileService personalProfileService,
            IEmployeeAddressService addressService,
            IEmployeeEmergencyContactService emergencyContactService,
            IEmployeeIdentificationRecordService identificationRecordService,
            IEmployeeProfileCompletionPolicy policy)
    {
        return new EmployeeProfileCompletionService(
            personalProfileService,
            addressService,
            emergencyContactService,
            identificationRecordService,
            policy);
    }

    private static Employee CreateEmployee()
    {
        return new Employee(
            Guid.NewGuid(),
            "EMP-COMPLETION-SERVICE-001",
            "Nguyễn Văn An",
            "an@example.com",
            "0901234567",
            new DateOnly(
                1995,
                5,
                10),
            new DateOnly(
                2025,
                1,
                1),
            "Phòng Nhân sự",
            "Chuyên viên",
            EmployeeStatus.Active);
    }

    private static EmployeePersonalProfileDetails
        CreatePersonalProfile(
            Guid employeeId)
    {
        return new EmployeePersonalProfileDetails(
            employeeId,
            HasProfile: true,
            PreferredName: "An",
            Gender: EmployeeGender.Male,
            Nationality: "Việt Nam",
            PlaceOfBirth: "Hà Nội");
    }

    private static EmployeeAddressBookDetails
        CreateAddressBook(
            Guid employeeId)
    {
        return new EmployeeAddressBookDetails(
            employeeId,
            PermanentAddress:
                CreateAddress(
                    EmployeeAddressType.Permanent),
            CurrentAddress:
                CreateAddress(
                    EmployeeAddressType.Current));
    }

    private static EmployeeAddressDetails CreateAddress(
        EmployeeAddressType type)
    {
        return new EmployeeAddressDetails(
            Guid.NewGuid(),
            type,
            type == EmployeeAddressType.Permanent
                ? "123 Lê Lợi"
                : "456 Nguyễn Huệ",
            Ward: "Phường Test",
            District: "Quận Test",
            Province: "Hà Nội",
            Country: "Việt Nam",
            PostalCode: "100000");
    }

    private sealed class RecordingCompletionPolicy
        : IEmployeeProfileCompletionPolicy
    {
        public EmployeeProfileCompletionResult
            Result
        {
            get;
            set;
        } =
            new(
                IsComplete: true,
                RequiresCompletion: false,
                MissingRequirements:
                    Array.Empty<
                        EmployeeProfileRequirement>());

        public int EvaluateCallCount
        {
            get;
            private set;
        }

        public EmployeeProfileCompletionData?
            LastData
        {
            get;
            private set;
        }

        public EmployeeProfileCompletionResult Evaluate(
            EmployeeProfileCompletionData data)
        {
            EvaluateCallCount++;

            LastData =
                data;

            return Result;
        }
    }

    private sealed class StubPersonalProfileService
        : IEmployeePersonalProfileService
    {
        public EmployeePersonalProfileDetails?
            Result
        {
            get;
            set;
        }

        public Task<EmployeePersonalProfileDetails>?
            ResultTask
        {
            get;
            set;
        }

        public Exception?
            Exception
        {
            get;
            set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public Guid?
            LastEmployeeId
        {
            get;
            private set;
        }

        public CancellationToken
            LastCancellationToken
        {
            get;
            private set;
        }

        public Task<EmployeePersonalProfileDetails>
            GetProfileAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            LastEmployeeId =
                employeeId;

            LastCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<
                    EmployeePersonalProfileDetails>(
                        Exception);
            }

            if (ResultTask is not null)
            {
                return ResultTask;
            }

            return Task.FromResult(
                Result
                ?? CreatePersonalProfile(
                    employeeId));
        }

        public Task<SaveEmployeePersonalProfileResult>
            SaveProfileAsync(
                SaveEmployeePersonalProfileRequest request,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubAddressService
        : IEmployeeAddressService
    {
        public EmployeeAddressBookDetails?
            Result
        {
            get;
            set;
        }

        public Task<EmployeeAddressBookDetails>?
            ResultTask
        {
            get;
            set;
        }

        public Exception?
            Exception
        {
            get;
            set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public Guid?
            LastEmployeeId
        {
            get;
            private set;
        }

        public CancellationToken
            LastCancellationToken
        {
            get;
            private set;
        }

        public Task<EmployeeAddressBookDetails>
            GetAddressesAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            LastEmployeeId =
                employeeId;

            LastCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<
                    EmployeeAddressBookDetails>(
                        Exception);
            }

            if (ResultTask is not null)
            {
                return ResultTask;
            }

            return Task.FromResult(
                Result
                ?? CreateAddressBook(
                    employeeId));
        }

        public Task<EmployeeAddressOperationResult>
            SaveAddressAsync(
                SaveEmployeeAddressRequest request,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<EmployeeAddressOperationResult>
            DeleteAddressAsync(
                Guid employeeId,
                EmployeeAddressType type,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubEmergencyContactService
        : IEmployeeEmergencyContactService
    {
        public IReadOnlyList<
            EmployeeEmergencyContactDetails>?
            Result
        {
            get;
            set;
        }

        public Task<IReadOnlyList<
            EmployeeEmergencyContactDetails>>?
            ResultTask
        {
            get;
            set;
        }

        public Exception?
            Exception
        {
            get;
            set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public Guid?
            LastEmployeeId
        {
            get;
            private set;
        }

        public CancellationToken
            LastCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<
            EmployeeEmergencyContactDetails>>
            GetContactsAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            LastEmployeeId =
                employeeId;

            LastCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<
                    IReadOnlyList<
                        EmployeeEmergencyContactDetails>>(
                            Exception);
            }

            if (ResultTask is not null)
            {
                return ResultTask;
            }

            IReadOnlyList<
                EmployeeEmergencyContactDetails>
                result =
                    Result
                    ?? Array.Empty<
                        EmployeeEmergencyContactDetails>();

            return Task.FromResult(
                result);
        }

        public Task<EmployeeEmergencyContactOperationResult>
            SaveContactAsync(
                SaveEmployeeEmergencyContactRequest request,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<EmployeeEmergencyContactOperationResult>
            DeleteContactAsync(
                Guid employeeId,
                Guid contactId,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubIdentificationRecordService
        : IEmployeeIdentificationRecordService
    {
        public IReadOnlyList<
            EmployeeIdentificationRecordDetails>?
            Result
        {
            get;
            set;
        }

        public Task<IReadOnlyList<
            EmployeeIdentificationRecordDetails>>?
            ResultTask
        {
            get;
            set;
        }

        public Exception?
            Exception
        {
            get;
            set;
        }

        public int GetCallCount
        {
            get;
            private set;
        }

        public Guid?
            LastEmployeeId
        {
            get;
            private set;
        }

        public CancellationToken
            LastCancellationToken
        {
            get;
            private set;
        }

        public Task<IReadOnlyList<
            EmployeeIdentificationRecordDetails>>
            GetRecordsAsync(
                Guid employeeId,
                CancellationToken cancellationToken = default)
        {
            GetCallCount++;

            LastEmployeeId =
                employeeId;

            LastCancellationToken =
                cancellationToken;

            if (Exception is not null)
            {
                return Task.FromException<
                    IReadOnlyList<
                        EmployeeIdentificationRecordDetails>>(
                            Exception);
            }

            if (ResultTask is not null)
            {
                return ResultTask;
            }

            IReadOnlyList<
                EmployeeIdentificationRecordDetails>
                result =
                    Result
                    ?? Array.Empty<
                        EmployeeIdentificationRecordDetails>();

            return Task.FromResult(
                result);
        }

        public Task<EmployeeIdentificationRecordOperationResult>
            SaveRecordAsync(
                SaveEmployeeIdentificationRecordRequest request,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<EmployeeIdentificationRecordOperationResult>
            DeleteRecordAsync(
                Guid employeeId,
                Guid recordId,
                CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
