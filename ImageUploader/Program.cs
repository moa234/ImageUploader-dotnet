using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
var app = builder.Build();

app.UseAntiforgery();

app.MapGet("/", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    return Results.Content(
        $"""
         <html>
         <head>
             <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
         </head>
         <body>
             <div class="container">
                 <h1>Upload Image</h1>
                 <form action="/upload" method="post" enctype="multipart/form-data">
                     <input name="{token.FormFieldName}" type="hidden" value="{token.RequestToken}" />
                     <input class="form-control mt-4" type="text" placeholder="Title" name="title" required>
                     <input class="form-control form-control-sm mt-2" type="file" name="file" required>
                     <br/>
                     <button type="submit" class="btn btn-primary">Upload</button>
                 </form>
             </div>
         </body>
         </html>
         """, "text/html");
});


app.MapPost("/upload", async (IFormFile file, [FromForm] string title) =>
{
    if (!IsImage(file))
    {
        return Results.BadRequest("Invalid file type");
    }

    // save image with unique id
    var id = Guid.NewGuid().ToString();
    Directory.CreateDirectory("picture");
    var path = Path.Combine("picture", id + Path.GetExtension(file.FileName));
    await using var stream = new FileStream(path, FileMode.Create);
    await file.CopyToAsync(stream);

    //create or read json file to store titles and image path mapped to specific id
    var jsonPath = Path.Combine("picture", "data.json");
    var data = new Dictionary<string, Dictionary<string, string>>();
    if (File.Exists(jsonPath))
    {
        var json = await File.ReadAllTextAsync(jsonPath);
        data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
    }

    // add new image data to json file
    data?.Add(id,
        new Dictionary<string, string> { { "title", title }, { "path", path }, { "contentType", file.ContentType } });
    await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(data));

    return await Task.FromResult(Results.Redirect($"/picture/{id}"));
});

app.MapGet("/pictureFile/{id}", async (string id) =>
{
    var data = GetImageData(id);
    if (data == null)
    {
        return Results.NotFound();
    }

    await using var stream = File.OpenRead(data["path"]);
    byte[] buffer = new byte[stream.Length];
    await stream.ReadAsync(buffer);
    return Results.File(buffer, data["contentType"]);
});

app.MapGet("/picture/{id}", (string id) =>
{
    var data = GetImageData(id);
    return data == null
        ? Results.NotFound()
        : Results.Content($"""
                           <html>
                           <head>
                               <link href="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH" crossorigin="anonymous">
                           </head>
                           <body>
                               <div class="container">
                                   <h1>{data["title"]}</h1>
                                   <img src="/pictureFile/{id}" class="img-fluid" width=500 />
                               </div>
                           </body>
                           </html>
                           """, contentType: "text/html");
});


app.Run();
return;

bool IsImage(IFormFile file)
{
    var allowedContentTypes = new[] { "image/png", "image/jpeg", "image/gif" };
    return allowedContentTypes.Contains(file.ContentType);
}

Dictionary<string, string>? GetImageData(string id)
{
    var jsonPath = Path.Combine("picture", "data.json");
    if (!File.Exists(jsonPath))
    {
        return null;
    }

    var json = File.ReadAllText(jsonPath);
    var data = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
    if (data != null && (!data.ContainsKey(id) || !File.Exists(data[id]["path"])))
    {
        return null;
    }

    return data?[id];
}