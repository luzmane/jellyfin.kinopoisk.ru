var KinopoiskConfigPage = {
    pluginUniqueId: "0417364b-5a93-4ad0-a5f0-b8756957cf80",
    loadConfiguration: function () {
      Dashboard.showLoadingMsg();
      ApiClient.getPluginConfiguration(KinopoiskConfigPage.pluginUniqueId).then(function (config) {
        document.querySelector('#txtToken').value = config.Token || '';
        document.querySelector('#top250MovieName').value = config.Top250MovieCollectionName;// || 'КинопоискТоп250';
        document.querySelector('#top250SeriesName').value = config.Top250SeriesCollectionName;// || 'КинопоискТоп250 (Сериалы)';
        document.querySelector('#chkCreateSeqCollections').checked = (config.ApiType == "kinopoisk.dev" && config.CreateSeqCollections);
        document.querySelector('#chkTop250InOneLib').checked = (config.ApiType == "kinopoisk.dev" && config.Top250InOneLib);
        document.querySelector('#kinopoiskUnofficial').checked = (config.ApiType == "kinopoiskapiunofficial.tech");
        // document.querySelector('#kinopoiskUnofficial').addEventListener('change', (event) => {
        //     if (event.currentTarget.checked) document.querySelectorAll('.kinopoiskDevOnly').forEach(item => item.style.display = 'none');
        // });
        document.querySelector('#kinopoiskDev').checked = (config.ApiType == "kinopoisk.dev");
        // document.querySelector('#kinopoiskDev').addEventListener('change', (event) => {
        //     if (event.currentTarget.checked) document.querySelectorAll('.kinopoiskDevOnly').forEach(item => item.style.display = '');
        // });
        Dashboard.hideLoadingMsg();
      });
    },
    saveConfiguration: function (config) {
      config.preventDefault();
      Dashboard.showLoadingMsg();
      ApiClient.getPluginConfiguration(KinopoiskConfigPage.pluginUniqueId).then(function (config) {
        config.Token = document.querySelector('#txtToken').value;
        config.CreateSeqCollections = document.querySelector('#chkCreateSeqCollections').checked;
        config.Top250InOneLib = document.querySelector('#chkTop250InOneLib').checked;
        config.Top250MovieCollectionName = document.querySelector('#top250MovieName').value;
        config.Top250SeriesCollectionName = document.querySelector('#top250SeriesName').value;
        config.ApiType = document.querySelector('input[name="radioAPI"]:checked').value;
        ApiClient.updatePluginConfiguration(KinopoiskConfigPage.pluginUniqueId, config)
          .then(function (result) { Dashboard.processPluginConfigurationUpdateResult(result); });
      });
    },
  };
  document.getElementById('kinopoiskConfigPage')
    .addEventListener('pageshow', function () { KinopoiskConfigPage.loadConfiguration(); });
  document.getElementById('kinopoiskConfigForm')
    .addEventListener('submit', function (e) { KinopoiskConfigPage.saveConfiguration(e); });
  