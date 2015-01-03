var APIURL = "http://api.purdue.io/odata/";

enum httpMethod {
	GET,
	POST
};

/**
 * This class exists to provide an easy way to make HTTP requests
 * And get JSON objects in response.
 */
class JsonRequest {

	/**
	 * Internal method to send an http request to some URL and
	 * return a JSON object via a Promise.
	 * 
	 * @param url The URL to send the request to
	 * @param httpMethod HTTP method used in request
	 * @param postData POST data to send, if the method used is post
	 */
	private static httpRequest<T>(url: string, method: httpMethod, postData?: any, username?: string, password?: string): Promise<T> {
		// I promise I'll do this. Pinky swear.
		return new Promise<T>((resolve, reject) => {
			var req = new XMLHttpRequest();
			switch (method) {
				case httpMethod.GET:
					req.open('GET', url);
					break;
				case httpMethod.POST:
					req.open('POST', url);
					break;
			}

			if (typeof username !== 'undefined' && typeof password !== 'undefined') {
				req.setRequestHeader("Authorization", "Basic " + btoa(username + ":" + password));
			}

			req.onload = function () {
				// This is called even on 404 etc
				// so check the status
				if (req.status == 200) {
					// Resolve the promise with the response text
					try {
						var result: any = JSON.parse(req.responseText);
					} catch (e) {
						reject(e);
						return;
					}
					// OData queries return their results in the 'value' element
					//if (typeof result.value !== 'undefined') {
					//	var tResult: T = result.value;
					//} else {
					//	var tResult: T = result;
					//}
					resolve(result);
				}
				else {
					// Otherwise reject with the status text
					// which will hopefully be a meaningful error
					reject(Error(req.statusText));
				}
			};

			// Handle network errors
			req.onerror = function () {
				reject(Error("Network Error"));
			};

			// Make the request
			switch (method) {
				case httpMethod.GET:
					req.send();
					break;
				case httpMethod.POST:
					req.send(postData);
					break;
			}
		});
	}

	/**
	 * A method to perform a GET HTTP request and parse resulting JSON
	 * 
	 * @param url URL to request
	 */
	public static httpGet<T>(url: string, username?: string, password?: string): Promise<T> {
		return JsonRequest.httpRequest<T>(url, httpMethod.GET, null, username, password);
	}

	/**
	 * A method to perform a POST HTTP request and parse resulting JSON
	 * 
	 * @param url URL to request
	 * @param postData JSON post data to send
	 */
	public static httpPost<T>(url: string, postData: any, username?: string, password?: string): Promise<T> {
		return JsonRequest.httpRequest<T>(url, httpMethod.POST, postData, username, password);
	}
}