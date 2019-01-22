import { JsonRequest } from "./JsonRequest";

class QueryTester {
	public static APIURL: string;
	private formElement: HTMLFormElement;
	private resultsElement: HTMLElement;
	private queriesRunning: number = 0;

	constructor() {
		this.formElement = <HTMLFormElement>document.querySelector("form");
		this.resultsElement = <HTMLElement>document.querySelector("div.results");

		this.formElement.addEventListener("submit", (ev) => {
			ev.preventDefault();
			this.runQuery();
		});
	}

	public runQuery() {
		var queryString = (<HTMLInputElement>this.formElement.querySelector("input")).value;
		this.queriesRunning++;
		(<HTMLElement>document.querySelector("div.globalProgressIndicator")).classList.remove("closed");
		JsonRequest.httpGet(QueryTester.APIURL + queryString).then((result) => {
			this.queriesRunning--;
			if (this.queriesRunning <= 0) {
				(<HTMLElement>document.querySelector("div.globalProgressIndicator")).classList.add("closed");
			}
			this.resultsElement.innerHTML = "";
			this.resultsElement.innerHTML = '<pre>' + JSON.stringify(result, undefined, 4) + '</pre>';
		}, (error) => {
			this.queriesRunning--;
			if (this.queriesRunning <= 0) {
				(<HTMLElement>document.querySelector("div.globalProgressIndicator")).classList.add("closed");
			}
			alert("Error in request: " + error);
		});
	}
}

// Set API url
QueryTester.APIURL = "";

window.onload = () => {
	new QueryTester();
}