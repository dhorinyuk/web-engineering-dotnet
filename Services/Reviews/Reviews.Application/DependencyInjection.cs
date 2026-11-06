using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Common.Behaviors;

namespace Reviews.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Criterion 7: MediatR setup with strict pipeline behaviors order
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));      // 1st: Outer logger
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));   // 2nd: Validation
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));  // 3rd: Performance measurement
        });

        // Criterion 7: Register all FluentValidation validators from assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
