using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnit;
using ReveilMusical.Application;
using ReveilMusical.Domain;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace ReveilMusical.Architecture.Tests;

public class LayeringTests
{
    private static readonly System.Reflection.Assembly DomainAssembly = typeof(UserId).Assembly;
    private static readonly System.Reflection.Assembly ApplicationAssembly = typeof(SendWakeUpUseCase).Assembly;
    private static readonly System.Reflection.Assembly MusicAssembly = typeof(ReveilMusical.Infrastructure.Music.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly NotificationsAssembly = typeof(ReveilMusical.Infrastructure.Notifications.ServiceCollectionExtensions).Assembly;
    private static readonly System.Reflection.Assembly UsersAssembly = typeof(ReveilMusical.Infrastructure.Users.ServiceCollectionExtensions).Assembly;

    private static readonly ArchUnitNET.Domain.Architecture Architecture = new ArchLoader()
        .LoadAssemblies(DomainAssembly, ApplicationAssembly, MusicAssembly, NotificationsAssembly, UsersAssembly)
        .Build();

    private static readonly IObjectProvider<IType> DomainLayer =
        Types().That().ResideInAssembly(DomainAssembly).As("Domain");

    private static readonly IObjectProvider<IType> ApplicationLayer =
        Types().That().ResideInAssembly(ApplicationAssembly).As("Application");

    private static readonly IObjectProvider<IType> InfrastructureLayer =
        Types().That()
            .ResideInAssembly(MusicAssembly, NotificationsAssembly, UsersAssembly)
            .As("Infrastructure");

    private static readonly IObjectProvider<IType> SystemNetHttp =
        Types().That().ResideInNamespace("System.Net.Http").As("System.Net.Http");

    [Fact]
    public void Domain_DoesNotDependOnAnyOtherProject()
    {
        Types().That().Are(DomainLayer).Should()
            .NotDependOnAny(ApplicationLayer)
            .AndShould().NotDependOnAny(InfrastructureLayer)
            .Because("the Domain is the innermost layer and must stay free of outward dependencies")
            .Check(Architecture);
    }

    [Fact]
    public void Domain_DoesNotDependOnSystemNetHttp()
    {
        Types().That().Are(DomainLayer).Should()
            .NotDependOnAny(SystemNetHttp)
            .Because("HTTP is an infrastructure detail, not a Domain concern")
            .Check(Architecture);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructure()
    {
        Types().That().Are(ApplicationLayer).Should()
            .NotDependOnAny(InfrastructureLayer)
            .Because("the Application layer only knows ports, never their adapters")
            .Check(Architecture);
    }

    [Fact]
    public void Application_DoesNotDependOnSystemNetHttp()
    {
        Types().That().Are(ApplicationLayer).Should()
            .NotDependOnAny(SystemNetHttp)
            .Because("HTTP is an infrastructure detail, not an Application concern")
            .Check(Architecture);
    }

    public static IEnumerable<object[]> InfrastructureAssemblies()
    {
        yield return [MusicAssembly];
        yield return [NotificationsAssembly];
        yield return [UsersAssembly];
    }

    [Theory]
    [MemberData(nameof(InfrastructureAssemblies))]
    public void Infrastructure_OnlyPublicTypeIsTheRegistrationExtension(System.Reflection.Assembly assembly)
    {
        Types().That().ResideInAssembly(assembly).And().ArePublic().Should()
            .HaveName("ServiceCollectionExtensions")
            .Because("every implementation detail must be internal; only the single AddXxx registration method is public")
            .Check(Architecture);
    }

    [Fact]
    public void DtoTypes_AreNeverPublic()
    {
        Types().That().HaveNameEndingWith("Dto").Should()
            .NotBePublic()
            .Because("DTOs are adapter-internal mapping details and must never leak outside their own assembly")
            .Check(Architecture);
    }

    [Theory]
    [InlineData("Itunes")]
    [InlineData("MusicBrainz")]
    [InlineData("Sms")]
    [InlineData("Email")]
    [InlineData("Push")]
    public void DomainAndApplication_HaveNoTypeNamedAfterAProviderOrChannel(string forbiddenNameFragment)
    {
        Types().That().Are(DomainLayer).Or().Are(ApplicationLayer).Should()
            .NotHaveNameContaining(forbiddenNameFragment)
            .Because("the business layers must stay ignorant of which music provider or notification channel is used")
            .Check(Architecture);
    }
}
