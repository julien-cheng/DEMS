export default {
  namespace: 'cases',
  state: [],
  reducers: {
    delete(state: any, { payload: id }: { payload: any }) {
      return state.filter((item: { id: any }) => item.id !== id);
    },
  },
};
