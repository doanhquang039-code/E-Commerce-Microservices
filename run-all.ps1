$projects = @(
    "backend\src\ApiGateway\ECommerce.ApiGateway\ECommerce.ApiGateway.csproj",
    "backend\src\Services\Identity\ECommerce.Identity.API\ECommerce.Identity.API.csproj",
    "backend\src\Services\Product\ECommerce.Product.API\ECommerce.Product.API.csproj",
    "backend\src\Services\Order\ECommerce.Order.API\ECommerce.Order.API.csproj",
    "backend\src\Services\Review\ECommerce.Review.API\ECommerce.Review.API.csproj",
    "backend\src\Services\Inventory\ECommerce.Inventory.API\ECommerce.Inventory.API.csproj",
    "backend\src\Services\Shipping\ECommerce.Shipping.API\ECommerce.Shipping.API.csproj"
)

Write-Host "Starting all E-Commerce Microservices..." -ForegroundColor Green

foreach ($project in $projects) {
    if (Test-Path $project) {
        Write-Host "Starting $project" -ForegroundColor Cyan
        Start-Process "dotnet" -ArgumentList "run --project `"$project`"" -NoNewWindow=$false
    } else {
        Write-Host "Project not found: $project" -ForegroundColor Red
    }
}

Write-Host "All services have been started in separate windows." -ForegroundColor Green
