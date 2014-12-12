class QueryTester {
    private elementContent: HTMLElement;
    private elementQuery: HTMLInputElement;

	constructor() {
		document.querySelector("nav button").addEventListener("click", () => {
            this.newQuery();
		});
        this.elementContent = <HTMLElement>document.querySelector("div.content");
        this.elementQuery = <HTMLInputElement>document.getElementById("queryBox");
	}

    public newQuery(): void {
        var xmlHttp = null;
        var query = this.elementQuery.value;

        xmlHttp = new XMLHttpRequest();
        xmlHttp.open("GET", DataSource.APIURL + query, true);
        xmlHttp.onreadystatechange = handlerFunction;
        xmlHttp.send(null);

        function handlerFunction() {
            if (xmlHttp.readyState == 4 && xmlHttp.status == 200) {
                document.getElementById("resultsBox").textContent = xmlHttp.responseText;
            }
            else {
                document.getElementById("resultsBox").textContent = xmlHttp.statusText;
            }
        }
	}
}