class DataSource {
    public static APIURL: string;
    private username: string;
    private password: string;

    constructor() {

    }
}

// Defining a default API url here - this can be overridden by Debug.ts
DataSource.APIURL = "http://api-dev.purdue.io/odata/";