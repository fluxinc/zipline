#lang racket/gui
(require file/zip)
(require file/sha1)

(define payload-dir
  (get-directory "Select payload directory."))

(define temp-dir (make-temporary-directory "~a"))

(define (create-payload)
  (current-directory payload-dir)

  (make-directory (build-path temp-dir "payload"))
  
  (map (lambda (f)
         (display (string-append (path->string f) "\n"))
         (copy-directory/files f (build-path temp-dir "payload" (file-name-from-path f))))
       (directory-list))

  (current-directory temp-dir)
  (zip  "payload.zip" "payload"))


(define (sign-payload)
  (let ((payload-path (build-path temp-dir "payload.zip")))
  (system (format "gpg --detach-sign --default-key support@fluxinc.ca ~s" (path->string payload-path)))))

(define (create-update-file)
  (let* ((sha256-hash (call-with-input-file (build-path temp-dir "payload.zip")
                        (lambda (in)
                          (bytes->hex-string
                           (sha256-bytes in)))))
         (final-name (string-append "warden-" sha256-hash))
         (zipped-name (string-append final-name ".zip")))
    
    (make-directory (build-path temp-dir final-name))
    
    (map (lambda (f) (rename-file-or-directory (build-path temp-dir (file-name-from-path f)) (build-path temp-dir final-name f)))
         '("payload.zip" "payload.zip.sig"))
    
    (zip zipped-name final-name)

    (copy-file (build-path temp-dir zipped-name)
               (build-path payload-dir "../" zipped-name))))

(define (warden-create)
  (display "Creating Payload.zip\n")
  (create-payload)
  (display "Signing Payload.zip\n")
  (sign-payload)
  (display "Creating update file\n")
  (create-update-file)
  (display "Cleaning up temp files.\n")
  (delete-directory/files temp-dir)
  (display "Completed. Please any key to quit.")
  (read-line)
  (exit))

(warden-create)

