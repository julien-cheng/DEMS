import { DocumentManagerPage } from './app.po';

describe('document-manager App', () => {
  let page: DocumentManagerPage;

  beforeEach(() => {
    page = new DocumentManagerPage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
