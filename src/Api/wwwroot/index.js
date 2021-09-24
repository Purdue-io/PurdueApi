function init() {
    var formEl = document.body.querySelector("form.query");
    var queryUrlEl = document.body.querySelector("div.queryBaseUrl");
    var sampleDropdownEl = document.body.querySelector("form.query select[name=samplequery]");
    var queryStringEl = document.body.querySelector("form.query textarea[name=queryString]");

    queryUrlEl.innerHTML = document.location.origin + "/odata/...";

    sampleDropdownEl.addEventListener("change", function(e) {
        queryStringEl.value = e.target.value;
    });

    formEl.addEventListener("submit", function(e) {
        e.preventDefault();
        fetch("/odata/" + encodeURI(queryStringEl.value))
            .then(resp => resp.json())
            .then(data => presentQueryResults(data));
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