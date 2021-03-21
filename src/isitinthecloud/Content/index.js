var app = new Vue({
    el: '#app',
    data: {
        loading: false,
        lookup: '',
        result: null
    },
    methods: {
        onSubmit: function () {
            this.loading = true;
            this.result = null;
            fetch('https://api.isitinthe.cloud/api/Lookup?lookup=' + this.lookup)
                .then(response => response.json())
                .then(data => {
                    this.result = data;
                })
                .finally(() => {
                    this.loading = false;
                });
        }
    }
});
