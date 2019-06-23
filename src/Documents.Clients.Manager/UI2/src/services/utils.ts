import { IFileIdentifier, IPathIdentifier } from '@/interfaces/identifiers.model';

import { IFileViewer } from '@/interfaces/file.model';
export default class Utils {
  static urlFromPathIdentifier(identifier: IPathIdentifier): string {
    return (
      `/manager/${identifier.organizationKey}/${identifier.folderKey}/` +
      encodeURIComponent(identifier.pathKey)
    );
  }
  static urlFromFileViewIdentifier(
    identifier: IFileIdentifier,
    path: string,
    view: IFileViewer,
  ): string {
    return (
      `/file/${identifier.organizationKey}/${identifier.folderKey}${
        path !== '' ? '/' + encodeURIComponent(path) : ''
      }/` +
      identifier.fileKey +
      '/' +
      view.viewerType
    );
  }
  static mapFileIconToAnt(icon: string): string {
    if (icon == 'file') {
      return 'file';
    } else if (icon == 'video') {
      return 'video-camera';
    } else {
      return 'file-unknown';
    }
  }
}
