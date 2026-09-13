// SPDX-License-Identifier: AGPL-3.0-or-later
using Microsoft.Extensions.DependencyInjection;

namespace Chimera.Abstractions.Interfaces;

public interface IServiceRegistration
{
    void RegisterServices(IServiceCollection services);
}
