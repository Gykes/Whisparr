import _ from 'lodash';
import { createSelector } from 'reselect';
import createAllSeriesSelector from './createAllSeriesSelector';

function createImportSeriesItemSelector() {
  return createSelector(
    (state, { id }) => id,
    (state) => state.addSeries,
    (state) => state.importSeries,
    createAllSeriesSelector(),
    (id, addSeries, importSeries, series) => {
      const item = _.find(importSeries.items, { id }) || {};
      const selectedSeries = item && item.selectedSeries;
      const isExistingSeries = !!selectedSeries && _.some(series, { tpdbId: selectedSeries.tpdbId });

      return {
        defaultMonitor: addSeries.defaults.monitor,
        defaultQualityProfileId: addSeries.defaults.qualityProfileId,
        defaultSeasonFolder: addSeries.defaults.seasonFolder,
        ...item,
        isExistingSeries
      };
    }
  );
}

export default createImportSeriesItemSelector;
