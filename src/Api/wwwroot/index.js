function init() {
    var formEl = document.body.querySelector("form.query");
    var queryUrlEl = document.body.querySelector("div.queryBaseUrl");
    var sampleDropdownEl = document.body.querySelector("form.query select[name=samplequery]");
    var queryStringEl = document.body.querySelector("form.query textarea[name=queryString]");
    var progressEl = document.body.querySelector("form.query progress");

    queryUrlEl.innerHTML = document.location.origin + "/odata/...";

    sampleDropdownEl.addEventListener("change", function(e) {
        queryStringEl.value = e.target.value;
    });

    formEl.addEventListener("submit", function(e) {
        e.preventDefault();
        progressEl.style.display = "";
        fetch("/odata/" + encodeURI(queryStringEl.value))
            .then(resp => {
                progressEl.style.display = "none";
                if (!resp.ok)
                {
                    alert("Error running query: " + resp.status + ": " + resp.statusText);
                }
                return resp.json();
            })
            .then(data => presentQueryResults(data))
            .catch(error => {
                alert(error)
                progressEl.style.display = "none";
            });
    });
}

function presentQueryResults(results)
{
    var queryResultsSectionEl = document.querySelector("section.queryResults");
    queryResultsSectionEl.style.display = "";
    var queryResultsPreEl = document.querySelector("section.queryResults pre");
    queryResultsPreEl.innerHTML = JSON.stringify(results, null, 4);
}

window.addEventListener("load", init);