#!/usr/bin/env python3
"""Fast static guard. Complements (does not replace) dotnet build/test/EF commands."""
from pathlib import Path
from xml.etree import ElementTree as ET
import json
import sys

root = Path(__file__).resolve().parents[1]
projects = {}
version = '10.0.15'
for name in ['Domain', 'Application', 'Infrastructure', 'Web']:
    path = root / 'src' / f'VehicleRegistry.{name}' / f'VehicleRegistry.{name}.csproj'
    tree = ET.parse(path)
    projects[name] = {Path(x.attrib['Include']).stem.split('.')[-1]
                      for x in tree.findall('.//ProjectReference')}
expected = {
    'Domain': set(),
    'Application': {'Domain'},
    'Infrastructure': {'Domain'},
    'Web': {'Application', 'Infrastructure'},
}
for name, deps in expected.items():
    assert projects[name] == deps, f'{name}: {projects[name]} != {deps}'

proj_text = '\n'.join(x.read_text() for x in root.rglob('*.csproj'))
assert 'Microsoft.AspNetCore.Identity.UI' not in proj_text
assert 'System.Security.Cryptography.Pkcs' not in proj_text
assert 'IRepository<' not in '\n'.join(x.read_text() for x in (root/'src/VehicleRegistry.Domain').rglob('*.cs'))
assert 'AddRazorPages' not in (root/'src/VehicleRegistry.Web/Program.cs').read_text()
assert 'MapRazorPages' not in (root/'src/VehicleRegistry.Web/Program.cs').read_text()
for name in ('Infrastructure', 'Web'):
    project = (root / f'src/VehicleRegistry.{name}/VehicleRegistry.{name}.csproj').read_text()
    assert f'Microsoft.EntityFrameworkCore.Design" Version="{version}"' in project, name
infra = (root/'src/VehicleRegistry.Infrastructure/VehicleRegistry.Infrastructure.csproj').read_text()
assert f'Microsoft.EntityFrameworkCore.Tools" Version="{version}"' in infra
assert f'Microsoft.EntityFrameworkCore.SqlServer" Version="{version}"' in infra
assert f'Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="{version}"' in infra
assert f'Microsoft.EntityFrameworkCore.Sqlite" Version="{version}"' in (root/'tests/VehicleRegistry.Tests/VehicleRegistry.Tests.csproj').read_text()
tools = json.loads((root/'.config/dotnet-tools.json').read_text())
assert tools['tools']['dotnet-ef']['version'] == version
assert 'ApplicationUser? Owner' in (root/'src/VehicleRegistry.Domain/Entities/Vehicle.cs').read_text()
assert 'ApplicationUser? CreatedBy' in (root/'src/VehicleRegistry.Domain/Entities/ServiceRecord.cs').read_text()
assert 'CreatedServiceRecords' in (root/'src/VehicleRegistry.Domain/Entities/ApplicationUser.cs').read_text()
assert 'WithMany(u => u.CreatedServiceRecords)' in (root/'src/VehicleRegistry.Infrastructure/Persistence/Configurations/EntityConfigurations.cs').read_text()
assert 'public string VehicleModel' in (root/'src/VehicleRegistry.Web/Models/Vehicles/VehicleForm.cs').read_text()
assert 'asp-for="VehicleModel"' in (root/'src/VehicleRegistry.Web/Views/Vehicles/Form.cshtml').read_text()
# Guard against a regression where the Razor view was only half-renamed:
# asp-for and asp-validation-for must refer to actual VehicleForm properties.
import re
vehicle_form_code = (root/'src/VehicleRegistry.Web/Models/Vehicles/VehicleForm.cs').read_text()
vehicle_form_view = (root/'src/VehicleRegistry.Web/Views/Vehicles/Form.cshtml').read_text()
vehicle_form_props = set(re.findall(r'public\s+[\w?<>]+\s+(\w+)\s*\{\s*get;', vehicle_form_code))
for tag_helper, field in re.findall(r'(asp-for|asp-validation-for)="([A-Za-z][A-Za-z0-9_]*)"', vehicle_form_view):
    assert field in vehicle_form_props, f'Unknown {tag_helper} in MVC VehicleForm: {field}'
assert 'asp-validation-for="VehicleModel"' in vehicle_form_view

assert 'string Model' not in (root/'src/VehicleRegistry.Web/Models/Vehicles/VehicleForm.cs').read_text()
assert list((root/'src/VehicleRegistry.Infrastructure/Persistence/Migrations').glob('*AddServiceRecordCreatorReference.cs'))
assert 'HasForeignKey("CreatedById")' in (root/'src/VehicleRegistry.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs').read_text()
print('PASS: Layer references, EF Design/Tools versions, domain navigation, migrations and MVC form names.')
print('NOTE: This script does not compile or run C#; use CI dotnet build/test/migrations checks.')
